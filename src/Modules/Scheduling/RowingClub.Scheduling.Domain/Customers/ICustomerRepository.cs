namespace RowingClub.Scheduling.Domain.Customers;

public interface ICustomerRepository
{
    Task<Customer?> GetByPhoneAsync(string phone, CancellationToken cancellationToken);

    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<Customer?> GetByMemberCodeAsync(string memberCode, CancellationToken cancellationToken);

    Task<bool> ExistsByMemberCodeAsync(string memberCode, CancellationToken cancellationToken);

    void Remove(Customer customer);

    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<List<Customer>> GetAllAsync(CancellationToken cancellationToken);

    void Add(Customer customer);
}
