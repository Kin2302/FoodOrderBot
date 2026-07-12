using FoodOrderBot.Application.AI;

namespace FoodOrderBot.Application.Contracts;

public interface IAiOrchestrator
{
    /// <summary>
    /// Entry point chính — classify intent → route đến đúng AI plugin → trả AiResponse.
    /// Worker gọi method này thay vì gọi IMessageParser trực tiếp.
    /// </summary>
    Task<AiResponse> ProcessMessageAsync(AiRequest request, CancellationToken ct = default);
}
