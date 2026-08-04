using System.Text.Json;
using FoodOrderBot.API.Hubs;
using FoodOrderBot.Application.AI;
using FoodOrderBot.Application.Contracts;
using FoodOrderBot.Application.Orders.Dtos;
using FoodOrderBot.Application.Parsing;
using FoodOrderBot.Domain.Entities;
using FoodOrderBot.Domain.Enums;
using FoodOrderBot.Domain.Interfaces;
using FoodOrderBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderBot.API.BackgroundServices;

/// <summary>
/// Background worker — consume WebhookTaskQueue và xử lý từng event:
/// 1. Dedup → 2. Upsert Customer → 3. Lưu RawMessage → 4. AI Orchestrator → 5. Xử lý theo Intent
/// </summary>
public class WebhookProcessingWorker(
    WebhookTaskQueue queue,
    IServiceScopeFactory scopeFactory,
    IHubContext<OrderHub> hubContext,
    ILogger<WebhookProcessingWorker> logger) : BackgroundService
{
    private const double ConfidenceThreshold = 0.8;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WebhookProcessingWorker started.");

        await foreach (var task in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(task, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi xử lý webhook: FbMessageId={FbMessageId}", task.FbMessageId);
            }
        }
    }

    private async Task ProcessAsync(WebhookTask task, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var rawMsgRepo = sp.GetRequiredService<IRawMessageRepository>();
        var customerRepo = sp.GetRequiredService<ICustomerRepository>();
        var orchestrator = sp.GetRequiredService<IAiOrchestrator>();
        var orderService = sp.GetRequiredService<IOrderService>();
        var messengerReply = sp.GetRequiredService<IMessengerReply>();
        var menuItemRepo = sp.GetRequiredService<IMenuItemRepository>();
        var db = sp.GetRequiredService<AppDbContext>();
        var config = sp.GetRequiredService<IConfiguration>();

        // 1. Dedup — Kiểm tra nhanh không cần Transaction
        if (await rawMsgRepo.ExistsByFbMessageIdAsync(task.FbMessageId, ct))
        {
            logger.LogInformation("[Worker] Dedup: FbMessageId={FbId} đã tồn tại, skip.", task.FbMessageId);
            return;
        }

        logger.LogInformation(
            "[Worker] Processing: Source={Source} | FbMessageId={FbId} | Content={Content}",
            task.Source, task.FbMessageId, task.Content[..Math.Min(50, task.Content.Length)]);

        // 2. Gọi AI Orchestrator (Xử lý ngoài DB)
        var aiResponse = await orchestrator.ProcessMessageAsync(
            new AiRequest(task.FbSenderId, task.ShopId, task.Content, task.Source), ct);

        logger.LogInformation("[Worker] AI done: Intent={Intent} | Confidence={Confidence}",
            aiResponse.Intent, aiResponse.Confidence);

        OrderDto? createdOrderDto = null;

        using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            try
            {
                var customer = await customerRepo.GetByFbSenderIdAsync(task.FbSenderId, ct);
                if (customer is null)
                {
                    customer = new Customer
                    {
                        Id = Guid.NewGuid(),
                        FbSenderId = task.FbSenderId,
                        Name = $"Khách {task.FbSenderId[..Math.Min(6, task.FbSenderId.Length)]}",
                        CreatedAt = DateTime.UtcNow
                    };
                    await customerRepo.AddAsync(customer, ct);
                    logger.LogInformation("[Worker] New customer created: {CustomerId}", customer.Id);
                }

                var rawMessage = new RawMessage
                {
                    Id = Guid.NewGuid(),
                    ShopId = task.ShopId,
                    FbSenderId = task.FbSenderId,
                    FbMessageId = task.FbMessageId,
                    FbPostId = task.FbPostId,
                    FbCommentId = task.FbCommentId,
                    Source = Enum.Parse<MessageSource>(task.Source, ignoreCase: true),
                    Content = task.Content,
                    CreatedAt = DateTime.UtcNow
                };

                if (aiResponse.ParseResult is not null)
                {
                    rawMessage.ParsedResult = JsonSerializer.Serialize(aiResponse.ParseResult);
                    rawMessage.ParseConfidence = (float)aiResponse.ParseResult.Confidence;
                }

                await rawMsgRepo.AddAsync(rawMessage, ct);

                if (aiResponse.Intent == AiIntent.PlaceOrder &&
                    aiResponse.ParseResult is { Confidence: >= ConfidenceThreshold } parseResult &&
                    parseResult.Items.Count > 0)
                {
                    var orderItems = await MapParsedItemsAsync(parseResult, task.ShopId, menuItemRepo, ct);

                    var createRequest = new CreateOrderRequest
                    {
                        ShopId = task.ShopId,
                        CustomerId = customer.Id,
                        RawMessageId = rawMessage.Id,
                        ReceiverName = parseResult.ReceiverName ?? customer.Name,
                        ReceiverPhone = parseResult.ReceiverPhone ?? "",
                        DeliveryAddress = parseResult.DeliveryAddress ?? "",
                        TotalAmount = orderItems.Sum(i => i.UnitPrice * i.Quantity),
                        ParseConfidence = (float)parseResult.Confidence,
                        UnclearParts = parseResult.UnclearParts,
                        Items = orderItems
                    };

                    createdOrderDto = await orderService.CreateDraftAsync(createRequest, ct);
                    logger.LogInformation("[Worker] Draft Order created: {OrderId} | Total={Total}",
                        createdOrderDto.Id, createdOrderDto.TotalAmount);
                }

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(ct);
                throw; 
            }
        }

        // 4. Xử lý Side-effects (SignalR + Facebook Reply) SAU KHi DB ĐÃ COMMIT AN TOÀN
        var shop = await db.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.Id == task.ShopId, ct);
        var pageAccessToken = !string.IsNullOrEmpty(shop?.FbAccessToken)
            ? shop.FbAccessToken
            : config["Facebook:PageAccessToken"] ?? "";

        if (createdOrderDto is not null)
        {
            // Push SignalR về Dashboard
            await hubContext.Clients.All.SendAsync("NewOrderReceived", new
            {
                orderId = createdOrderDto.Id,
                status = createdOrderDto.Status.ToString(),
                customerName = createdOrderDto.CustomerName,
                totalAmount = createdOrderDto.TotalAmount,
                itemCount = createdOrderDto.Items.Count,
                confidence = createdOrderDto.ParseConfidence,
                createdAt = createdOrderDto.CreatedAt,
                intent = aiResponse.Intent.ToString(),
                upsellSuggestions = aiResponse.Suggestions
            }, ct);

            // Gửi Tracking Link qua Messenger
            if (!string.IsNullOrEmpty(pageAccessToken))
            {
                await messengerReply.SendTrackingLinkAsync(
                    task.FbSenderId, createdOrderDto.Id.ToString(), createdOrderDto.TrackingToken, pageAccessToken, ct);
            }
        }
        else
        {
            // Gửi câu trả lời thường cho các Intent khác
            if (!string.IsNullOrEmpty(pageAccessToken) && !string.IsNullOrEmpty(aiResponse.ReplyText))
            {
                await messengerReply.SendTextAsync(task.FbSenderId, aiResponse.ReplyText, pageAccessToken, ct);
            }

            logger.LogInformation("[Worker] Sent reply for Intent={Intent}: {Reply}",
                aiResponse.Intent,
                aiResponse.ReplyText[..Math.Min(80, aiResponse.ReplyText.Length)]);
        }
    }

    /// <summary>Map ParsedOrderItem từ AI → CreateOrderItemRequest, resolve giá từ DB.</summary>
    private static async Task<List<CreateOrderItemRequest>> MapParsedItemsAsync(
        ParseResultDto parseResult,
        Guid shopId,
        IMenuItemRepository menuItemRepo,
        CancellationToken ct)
    {
        var menuItems = (await menuItemRepo.GetByShopIdAsync(shopId, ct)).ToList();
        var orderItems = new List<CreateOrderItemRequest>();

        foreach (var parsed in parseResult.Items)
        {
            MenuItem? matched = null;

            // Ưu tiên 1: match bằng MenuItemId (nếu AI trả về)
            if (parsed.MenuItemId.HasValue)
                matched = menuItems.FirstOrDefault(m => m.Id == parsed.MenuItemId.Value);

            // Ưu tiên 2: fuzzy match bằng tên
            if (matched is null && !string.IsNullOrEmpty(parsed.Name))
                matched = menuItems.FirstOrDefault(m =>
                    m.Name.Contains(parsed.Name, StringComparison.OrdinalIgnoreCase) ||
                    parsed.Name.Contains(m.Name, StringComparison.OrdinalIgnoreCase));

            orderItems.Add(new CreateOrderItemRequest
            {
                MenuItemId = matched?.Id ?? Guid.Empty,
                ItemName = matched?.Name ?? parsed.Name,
                UnitPrice = matched?.Price ?? 0,
                Quantity = parsed.Quantity,
                Note = parsed.Note
            });

            if (matched is null)
                parseResult.UnclearParts.Add($"Không tìm thấy món '{parsed.Name}' trong menu");
        }

        return orderItems;
    }
}
