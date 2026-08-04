namespace FoodOrderBot.Application.Analytics;

// ── Summary DTO ──────────────────────────────────────────────────────────────

public class AnalyticsSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<DailyRevenueDto> DailyRevenue { get; set; } = [];
    public List<TopMenuItemDto> TopMenuItems { get; set; } = [];
    public List<HourlyDistributionDto> HourlyDistribution { get; set; } = [];
    public List<OrderStatusBreakdownDto> StatusBreakdown { get; set; } = [];
}

public record DailyRevenueDto(string Date, decimal Revenue, int OrderCount);
public record TopMenuItemDto(string Name, int TotalQuantity, decimal TotalRevenue);
public record HourlyDistributionDto(int Hour, int OrderCount);
public record OrderStatusBreakdownDto(string Status, int Count);

// ── AI Stats DTO ─────────────────────────────────────────────────────────────

public class AiStatsDto
{
    public int TotalMessages { get; set; }
    public int ParsedSuccessfully { get; set; }
    public int NeedsClarification { get; set; }
    public double AverageConfidence { get; set; }
    public List<IntentDistributionDto> IntentDistribution { get; set; } = [];
    public int ComplaintsTotal { get; set; }
    public int ComplaintsNeedingAttention { get; set; }
}

public record IntentDistributionDto(string Intent, int Count);
