namespace FoodOrderBot.Domain.Entities;

public class Shop
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FbPageId { get; set; } = string.Empty;
    public string FbAccessToken { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<MenuItem> MenuItems { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<RawMessage> RawMessages { get; set; } = [];
}
