namespace FoodOrderBot.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }

    /// <summary>
    /// Snapshot tên món tại thời điểm đặt — tránh bị ảnh hưởng nếu menu đổi tên sau
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Snapshot giá tại thời điểm đặt — giá chốt cứng, không thay đổi theo thời gian
    /// </summary>
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }
    public string? Note { get; set; }  // "ít cơm", "nhiều chả", ...

    // Navigation
    public Order Order { get; set; } = null!;
    public MenuItem MenuItem { get; set; } = null!;
}
