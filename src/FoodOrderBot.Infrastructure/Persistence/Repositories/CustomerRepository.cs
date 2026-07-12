using FoodOrderBot.Domain.Entities;
using FoodOrderBot.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderBot.Infrastructure.Persistence.Repositories;

public class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    public async Task<Customer?> GetByFbSenderIdAsync(string fbSenderId, CancellationToken ct = default)
        => await db.Customers.FirstOrDefaultAsync(c => c.FbSenderId == fbSenderId, ct);

    public async Task AddAsync(Customer customer, CancellationToken ct = default)
        => await db.Customers.AddAsync(customer, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
