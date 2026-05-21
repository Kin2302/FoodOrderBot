using FoodOrderBot.Application.Orders.Dtos;
using FoodOrderBot.Domain.Enums;

namespace FoodOrderBot.Application.Contracts;

public interface IOrderService
{
    Task<OrderDto> CreateDraftAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<OrderDto> ConfirmAsync(Guid orderId, CancellationToken ct = default);
    Task<OrderDto> UpdateStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken ct = default);
    Task<OrderDto> UpdateOrderAsync(Guid orderId, UpdateOrderRequest request, CancellationToken ct = default);
    Task<IEnumerable<OrderDto>> GetAllByShopAsync(Guid shopId, CancellationToken ct = default);
    Task<OrderDto?> GetByTrackingTokenAsync(string token, CancellationToken ct = default);
}
