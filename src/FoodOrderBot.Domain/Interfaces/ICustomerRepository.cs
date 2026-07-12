using FoodOrderBot.Domain.Entities;

namespace FoodOrderBot.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByFbSenderIdAsync(string fbSenderId, CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
