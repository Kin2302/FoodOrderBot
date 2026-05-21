namespace FoodOrderBot.Domain.Entities;

public class MenuItem
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Shop Shop { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
