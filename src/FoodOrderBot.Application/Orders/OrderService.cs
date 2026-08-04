using System.Security.Cryptography;
using System.Text.Json;
using FoodOrderBot.Application.Contracts;
using FoodOrderBot.Application.Orders.Dtos;
using FoodOrderBot.Domain.Entities;
using FoodOrderBot.Domain.Enums;
using FoodOrderBot.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FoodOrderBot.Application.Orders;

/// <summary>
/// Implement IOrderService — quản lý toàn bộ vòng đời đơn hàng.
/// </summary>
public class OrderService(
    IOrderRepository orderRepo,
    ILogger<OrderService> logger) : IOrderService
{
    // ─── CreateDraft ──────────────────────────────────────────────────────────

    public async Task<OrderDto> CreateDraftAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var order = new Order
        {
            Id            = Guid.NewGuid(),
            ShopId        = request.ShopId,
            CustomerId    = request.CustomerId,
            RawMessageId  = request.RawMessageId,
            Status        = OrderStatus.Draft,
            ReceiverName  = request.ReceiverName,
            ReceiverPhone = request.ReceiverPhone,
            DeliveryAddress = request.DeliveryAddress,
            PaymentMethod = request.PaymentMethod,
            TotalAmount   = request.TotalAmount,
            TrackingToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                                   .Replace("+", "-").Replace("/", "_").Replace("=", ""),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = request.Items.Select(i => new OrderItem
            {
                Id         = Guid.NewGuid(),
                MenuItemId = i.MenuItemId,
                ItemName   = i.ItemName,
                UnitPrice  = i.UnitPrice,
                Quantity   = i.Quantity,
                Note       = i.Note
            }).ToList()
        };

        await orderRepo.AddAsync(order, ct);
        //await orderRepo.SaveChangesAsync(ct);

        logger.LogInformation("Draft order {OrderId} created for shop {ShopId}", order.Id, order.ShopId);

        return MapToDto(order, parseConfidence: request.ParseConfidence, unclearParts: request.UnclearParts);
    }

    // ─── Confirm ─────────────────────────────────────────────────────────────

    public async Task<OrderDto> ConfirmAsync(Guid orderId, CancellationToken ct = default)
        => await UpdateStatusAsync(orderId, OrderStatus.Confirmed, ct);

    // ─── UpdateStatus ─────────────────────────────────────────────────────────

    public async Task<OrderDto> UpdateStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken ct = default)
    {
        var order = await GetOrderOrThrowAsync(orderId, ct);

        OrderStateMachine.ThrowIfInvalidTransition(order.Status, newStatus);

        order.Status    = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await orderRepo.UpdateAsync(order, ct);
        await orderRepo.SaveChangesAsync(ct);

        logger.LogInformation("Order {OrderId} status changed to {Status}", orderId, newStatus);
        return MapToDto(order);
    }

    // ─── UpdateOrder ──────────────────────────────────────────────────────────

    public async Task<OrderDto> UpdateOrderAsync(Guid orderId, UpdateOrderRequest request, CancellationToken ct = default)
    {
        var order = await GetOrderOrThrowAsync(orderId, ct);

        if (order.Status != OrderStatus.Draft)
            throw new InvalidOperationException(
                $"Chỉ có thể chỉnh sửa đơn hàng ở trạng thái Draft. Trạng thái hiện tại: {order.Status}");

        if (request.ReceiverName    is not null) order.ReceiverName    = request.ReceiverName;
        if (request.ReceiverPhone   is not null) order.ReceiverPhone   = request.ReceiverPhone;
        if (request.DeliveryAddress is not null) order.DeliveryAddress = request.DeliveryAddress;
        if (request.PaymentMethod   is not null) order.PaymentMethod   = request.PaymentMethod;
        if (request.Note            is not null) order.Note            = request.Note;

        if (request.Items is { Count: > 0 })
        {
            order.Items = request.Items.Select(i => new OrderItem
            {
                Id         = Guid.NewGuid(),
                OrderId    = order.Id,
                MenuItemId = i.MenuItemId,
                ItemName   = i.ItemName,
                UnitPrice  = i.UnitPrice,
                Quantity   = i.Quantity,
                Note       = i.Note
            }).ToList();

            order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        }

        order.UpdatedAt = DateTime.UtcNow;

        await orderRepo.UpdateAsync(order, ct);
        await orderRepo.SaveChangesAsync(ct);

        logger.LogInformation("Order {OrderId} updated", orderId);
        return MapToDto(order);
    }

    // ─── Queries ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<OrderDto>> GetAllByShopAsync(Guid shopId, CancellationToken ct = default)
    {
        var orders = await orderRepo.GetByShopIdAsync(shopId, ct);
        return orders.Select(o => MapToDtoFromDb(o));
    }

    public async Task<OrderDto?> GetByTrackingTokenAsync(string token, CancellationToken ct = default)
    {
        var order = await orderRepo.GetByTrackingTokenAsync(token, ct);
        return order is null ? null : MapToDto(order);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Order> GetOrderOrThrowAsync(Guid orderId, CancellationToken ct)
    {
        var order = await orderRepo.GetByIdAsync(orderId, ct)
            ?? throw new KeyNotFoundException($"Không tìm thấy đơn hàng với Id: {orderId}");
        return order;
    }

    /// <summary>MapToDto khi vừa tạo Draft — confidence truyền trực tiếp từ AI response.</summary>
    private static OrderDto MapToDto(Order o, float? parseConfidence = null, List<string>? unclearParts = null)
        => BuildDto(o, parseConfidence, unclearParts);

    /// <summary>MapToDto khi load từ DB — đọc ParseConfidence + UnclearParts từ RawMessage.</summary>
    private static OrderDto MapToDtoFromDb(Order o)
    {
        var confidence  = o.RawMessage?.ParseConfidence;
        var unclearParts = ExtractUnclearParts(o.RawMessage?.ParsedResult);
        return BuildDto(o, confidence, unclearParts);
    }

    private static List<string> ExtractUnclearParts(string? parsedResultJson)
    {
        if (string.IsNullOrWhiteSpace(parsedResultJson)) return [];
        try
        {
            using var doc = JsonDocument.Parse(parsedResultJson);
            if (doc.RootElement.TryGetProperty("unclearParts", out var arr)
                && arr.ValueKind == JsonValueKind.Array)
            {
                return arr.EnumerateArray()
                          .Select(e => e.GetString() ?? string.Empty)
                          .Where(s => !string.IsNullOrEmpty(s))
                          .ToList();
            }
        }
        catch { /* malformed JSON — bỏ qua */ }
        return [];
    }

    private static OrderDto BuildDto(Order o, float? parseConfidence, List<string>? unclearParts)
        => new()
        {
            Id              = o.Id,
            Status          = o.Status,
            ReceiverName    = o.ReceiverName,
            ReceiverPhone   = o.ReceiverPhone,
            DeliveryAddress = o.DeliveryAddress,
            PaymentMethod   = o.PaymentMethod,
            PaymentStatus   = o.PaymentStatus,
            TotalAmount     = o.TotalAmount,
            TrackingToken   = o.TrackingToken,
            ParseConfidence = parseConfidence,
            UnclearParts    = unclearParts ?? [],
            Note            = o.Note,
            CustomerName    = o.Customer?.Name ?? string.Empty,
            RawMessageContent = o.RawMessage?.Content ?? string.Empty,
            CreatedAt       = o.CreatedAt,
            UpdatedAt       = o.UpdatedAt,
            Items = o.Items?.Select(i => new OrderItemDto
            {
                Id         = i.Id,
                MenuItemId = i.MenuItemId,
                ItemName   = i.ItemName,
                UnitPrice  = i.UnitPrice,
                Quantity   = i.Quantity,
                Note       = i.Note
            }).ToList() ?? []
        };
}
