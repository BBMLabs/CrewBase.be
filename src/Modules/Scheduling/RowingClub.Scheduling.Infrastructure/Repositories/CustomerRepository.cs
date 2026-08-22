using Microsoft.EntityFrameworkCore;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Infrastructure.Persistence;

namespace RowingClub.Scheduling.Infrastructure.Repositories;

public sealed class CustomerRepository(TenantDbContext context) : ICustomerRepository
{
    public Task<Customer?> GetByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        // Telefon kolonu şifreli; arama, normalize edilmiş numaranın HMAC blind index'i üzerinden.
        var phoneIndex = context.ComputePhoneIndex(phone);
        return context.Customers.FirstOrDefaultAsync(
            c => EF.Property<string>(c, TenantDbContext.PhoneIndexColumn) == phoneIndex, cancellationToken);
    }

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var emailIndex = context.ComputeEmailIndex(email);
        return context.Customers.FirstOrDefaultAsync(
            c => EF.Property<string?>(c, TenantDbContext.EmailIndexColumn) == emailIndex, cancellationToken);
    }

    public Task<Customer?> GetByMemberCodeAsync(string memberCode, CancellationToken cancellationToken) =>
        context.Customers.FirstOrDefaultAsync(c => c.MemberCode == memberCode, cancellationToken);

    public Task<bool> ExistsByMemberCodeAsync(string memberCode, CancellationToken cancellationToken) =>
        context.Customers.AnyAsync(c => c.MemberCode == memberCode, cancellationToken);

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Remove(Customer customer) => context.Customers.Remove(customer);

    public Task<List<Customer>> GetAllAsync(CancellationToken cancellationToken) =>
        context.Customers.ToListAsync(cancellationToken);

    public void Add(Customer customer) => context.Customers.Add(customer);
}
