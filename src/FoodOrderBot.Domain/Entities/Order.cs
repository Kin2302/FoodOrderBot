using FoodOrderBot.Domain.Enums;

namespace FoodOrderBot.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid RawMessageId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    // Thông tin người nhận (do AI bóc tách)
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;

    // Thanh toán
    public string PaymentMethod { get; set; } = "COD";
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Token ngẫu nhiên 32-byte — dùng để tạo link tracking public cho khách.
    /// Không expose OrderId ra ngoài.
    /// </summary>
    public string TrackingToken { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Shop Shop { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public RawMessage RawMessage { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
}
