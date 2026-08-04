using FoodOrderBot.Application.Analytics;

namespace FoodOrderBot.Application.Contracts;

public interface IAnalyticsService
{
    Task<AnalyticsSummaryDto> GetSummaryAsync(Guid shopId, int days = 30, CancellationToken ct = default);
    Task<AiStatsDto> GetAiStatsAsync(Guid shopId, int days = 30, CancellationToken ct = default);
}
