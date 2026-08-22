using MediatR;
using RowingClub.Scheduling.Domain.Customers;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record GetCustomersQuery : IRequest<List<CustomerDto>>;

public sealed record CustomerDto(
    Guid Id,
    string FullName,
    string Phone,
    string? Email,
    int Level,
    DateTimeOffset CreatedAtUtc);

public sealed class GetCustomersQueryHandler(ICustomerRepository customerRepository)
    : IRequestHandler<GetCustomersQuery, List<CustomerDto>>
{
    public async Task<List<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var customers = await customerRepository.GetAllAsync(cancellationToken);

        return customers
            .OrderBy(c => c.FullName)
            .Select(c => new CustomerDto(c.Id, c.FullName, c.Phone, c.Email, c.Level, c.CreatedAtUtc))
            .ToList();
    }
}
