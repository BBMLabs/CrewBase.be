using MediatR;
using RowingClub.Scheduling.Domain.Customers;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record GetCustomersQuery(string? Search = null, Guid? BranchId = null) : IRequest<List<CustomerDto>>;

public sealed record CustomerDto(
    Guid Id,
    string FullName,
    string Phone,
    string? Email,
    int Level,
    Guid? BranchId,
    bool IsBlocked,
    DateTimeOffset CreatedAtUtc,
    string? MemberCode);

public sealed class GetCustomersQueryHandler(ICustomerRepository customerRepository)
    : IRequestHandler<GetCustomersQuery, List<CustomerDto>>
{
    public async Task<List<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var customers = await customerRepository.GetAllAsync(cancellationToken);

        // Ad/telefon/e-posta veritabanında şifreli saklanır; kısmi arama yalnızca uygulama
        // belleğinde (zaten çözülmüş listede) yapılabilir - LIKE sorgusu şifreli kolonda çalışmaz.
        if (request.BranchId is { } branchId)
            customers = customers.Where(c => c.BranchId == branchId).ToList();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            customers = customers.Where(c =>
                c.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.Phone.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (c.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        return customers
            .OrderBy(c => c.FullName)
            .Select(c => new CustomerDto(c.Id, c.FullName, c.Phone, c.Email, c.Level, c.BranchId, c.IsBlocked, c.CreatedAtUtc, c.MemberCode))
            .ToList();
    }
}
