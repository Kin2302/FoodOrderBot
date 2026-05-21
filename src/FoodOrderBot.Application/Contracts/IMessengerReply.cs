namespace FoodOrderBot.Application.Contracts;

public interface IMessengerReply
{
    Task SendTextAsync(string fbSenderId, string message, string pageAccessToken, CancellationToken ct = default);
    Task SendTrackingLinkAsync(string fbSenderId, string orderId, string trackingToken, string pageAccessToken, CancellationToken ct = default);
}
