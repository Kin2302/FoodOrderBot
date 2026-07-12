using FoodOrderBot.Application.Contracts;
using FoodOrderBot.Application.Parsing;
using FoodOrderBot.Infrastructure.AI.Plugins;

namespace FoodOrderBot.Infrastructure.SemanticKernel;

/// <summary>
/// Thin wrapper giữ backward compatibility với IMessageParser.
/// Delegate thực sự sang OrderParserPlugin (không có conversation context).
/// Dùng khi cần gọi IMessageParser trực tiếp mà không qua Orchestrator.
/// </summary>
public class MessageParserService(OrderParserPlugin orderParser) : IMessageParser
{
    public Task<ParseResultDto> ParseAsync(string rawText, Guid shopId, CancellationToken ct = default)
        // Không có conversation history — dùng list rỗng
        => orderParser.ParseAsync(rawText, shopId, [], ct);
}
