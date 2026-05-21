namespace FoodOrderBot.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string FbSenderId { get; set; } = string.Empty;  // Facebook PSID
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Order> Orders { get; set; } = [];
}   
