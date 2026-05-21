using FoodOrderBot.Domain.Enums;

namespace FoodOrderBot.Application.Orders.Dtos;

public class CreateOrderRequest
{
    public Guid ShopId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid RawMessageId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "COD";
    public decimal TotalAmount { get; set; }
    public float ParseConfidence { get; set; }
    public List<string> UnclearParts { get; set; } = [];
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

public class CreateOrderItemRequest
{
    public Guid MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
}

public class UpdateOrderRequest
{
    public string? ReceiverName { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? PaymentMethod { get; set; }
    public List<CreateOrderItemRequest>? Items { get; set; }
}
