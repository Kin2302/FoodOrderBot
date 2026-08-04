using FoodOrderBot.Application.Contracts;
using FoodOrderBot.Domain.Enums;
using FoodOrderBot.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FoodOrderBot.Application.Analytics;

/// <summary>
/// Aggregate analytics data từ Orders, RawMessages, ConversationMessages.
/// Tất cả aggregation thực hiện in-memory sau khi query theo date range.
/// </summary>
public class AnalyticsService(
    IOrderRepository orderRepo,
    IRawMessageRepository rawMessageRepo,
    IConversationRepository conversationRepo,
    ILogger<AnalyticsService> logger) : IAnalyticsService
{
    public async Task<AnalyticsSummaryDto> GetSummaryAsync(
        Guid shopId, int days = 30, CancellationToken ct = default)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        var to = DateTime.UtcNow;

        var orders = (await orderRepo.GetByShopIdAndDateRangeAsync(shopId, from, to, ct)).ToList();

        logger.LogInformation("[Analytics] Summary: {Count} orders in last {Days} days", orders.Count, days);

        var completedOrders = orders.Where(o => o.Status == OrderStatus.Completed).ToList();

        // ── KPI Cards ────────────────────────────────────────────────────────
        var totalRevenue = completedOrders.Sum(o => o.TotalAmount);
        var averageOrderValue = completedOrders.Count > 0
            ? totalRevenue / completedOrders.Count
            : 0;

        // ── Daily Revenue (line chart) ───────────────────────────────────────
        var dailyRevenue = completedOrders
            .GroupBy(o => o.CreatedAt.ToString("yyyy-MM-dd"))
            .Select(g => new DailyRevenueDto(
                g.Key,
                g.Sum(o => o.TotalAmount),
                g.Count()))
            .OrderBy(d => d.Date)
            .ToList();

        // Thêm ngày không có đơn (để chart liền mạch)
        var allDates = Enumerable.Range(0, days)
            .Select(i => from.AddDays(i).ToString("yyyy-MM-dd"))
            .ToList();
        var dailyRevenueMap = dailyRevenue.ToDictionary(d => d.Date);
        dailyRevenue = allDates
            .Select(date => dailyRevenueMap.TryGetValue(date, out var d)
                ? d
                : new DailyRevenueDto(date, 0, 0))
            .ToList();

        // ── Top Menu Items (bar chart, top 5) ────────────────────────────────
        var topMenuItems = orders
            .Where(o => o.Items != null)
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ItemName)
            .Select(g => new TopMenuItemDto(
                g.Key,
                g.Sum(i => i.Quantity),
                g.Sum(i => i.UnitPrice * i.Quantity)))
            .OrderByDescending(t => t.TotalQuantity)
            .Take(5)
            .ToList();

        // ── Hourly Distribution (area chart) ─────────────────────────────────
        var hourlyDistribution = Enumerable.Range(0, 24)
            .Select(h => new HourlyDistributionDto(
                h,
                orders.Count(o => o.CreatedAt.Hour == h)))
            .ToList();

        // ── Status Breakdown (pie chart) ─────────────────────────────────────
        var statusBreakdown = orders
            .GroupBy(o => o.Status.ToString())
            .Select(g => new OrderStatusBreakdownDto(g.Key, g.Count()))
            .ToList();

        return new AnalyticsSummaryDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = orders.Count,
            CompletedOrders = completedOrders.Count,
            CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
            AverageOrderValue = averageOrderValue,
            DailyRevenue = dailyRevenue,
            TopMenuItems = topMenuItems,
            HourlyDistribution = hourlyDistribution,
            StatusBreakdown = statusBreakdown,
        };
    }

    public async Task<AiStatsDto> GetAiStatsAsync(
        Guid shopId, int days = 30, CancellationToken ct = default)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        var to = DateTime.UtcNow;

        // Lấy RawMessages để tính parse stats
        var rawMessages = (await rawMessageRepo.GetByShopIdAndDateRangeAsync(shopId, from, to, ct)).ToList();

        var withConfidence = rawMessages.Where(r => r.ParseConfidence.HasValue).ToList();
        var parsedSuccessfully = withConfidence.Count(r => r.ParseConfidence >= 0.8);
        var needsClarification = withConfidence.Count(r => r.ParseConfidence < 0.8);
        var averageConfidence = withConfidence.Count > 0
            ? withConfidence.Average(r => r.ParseConfidence!.Value)
            : 0.0;

        // Lấy ConversationMessages để tính intent distribution + complaints
        var conversations = await conversationRepo.GetByShopIdAndDateRangeAsync(shopId, from, to, ct);

        var intentDistribution = conversations
            .Where(c => !string.IsNullOrEmpty(c.Intent))
            .GroupBy(c => c.Intent!)
            .Select(g => new IntentDistributionDto(g.Key, g.Count()))
            .OrderByDescending(i => i.Count)
            .ToList();

        var complaints = conversations.Where(c => c.Intent == "Complaint").ToList();

        return new AiStatsDto
        {
            TotalMessages = rawMessages.Count,
            ParsedSuccessfully = parsedSuccessfully,
            NeedsClarification = needsClarification,
            AverageConfidence = averageConfidence,
            IntentDistribution = intentDistribution,
            ComplaintsTotal = complaints.Count,
            ComplaintsNeedingAttention = complaints.Count(c => c.NeedsAttention),
        };
    }
}
