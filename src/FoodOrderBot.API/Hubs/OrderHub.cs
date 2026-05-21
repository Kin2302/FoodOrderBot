using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FoodOrderBot.API.Hubs;

/// <summary>
/// SignalR Hub — kênh realtime giữa server và Dashboard của chủ quán.
/// Client connect vào "/hubs/orders" để nhận thông báo đơn mới.
/// </summary>
[Authorize]
public class OrderHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", new
        {
            connectionId = Context.ConnectionId,
            message = "Kết nối Dashboard thành công!"
        });
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        return base.OnDisconnectedAsync(exception);
    }
}
