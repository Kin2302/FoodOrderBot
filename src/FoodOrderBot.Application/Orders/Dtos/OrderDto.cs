using FoodOrderBot.Domain.Enums;

namespace FoodOrderBot.Application.Orders.Dtos;

public class OrderDto
{
    public Guid Id { get; set; }
    public OrderStatus Status { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public PaymentStatus PaymentStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public string TrackingToken { get; set; } = string.Empty;
    public float? ParseConfidence { get; set; }
    public List<string> UnclearParts { get; set; } = [];
    public List<OrderItemDto> Items { get; set; } = [];
    public string CustomerName { get; set; } = string.Empty;
    public string RawMessageContent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
}
