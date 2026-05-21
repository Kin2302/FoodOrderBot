using FoodOrderBot.Application.Parsing;

namespace FoodOrderBot.Application.Contracts;

public interface IMessageParser
{
    /// <summary>
    /// Phân tích chuỗi văn bản tự nhiên của khách → ParseResultDto (JSON order + confidence)
    /// </summary>
    Task<ParseResultDto> ParseAsync(string rawText, Guid shopId, CancellationToken ct = default);
}
