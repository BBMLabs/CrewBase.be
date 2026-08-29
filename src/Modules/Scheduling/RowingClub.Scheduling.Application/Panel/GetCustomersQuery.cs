using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Scheduling.Domain.Customers;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record GetCustomersQuery(
    string? Search = null, Guid? BranchId = null, int Page = 1, int PageSize = 25)
    : IRequest<PagedResult<CustomerDto>>;

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
    : IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var customers = await customerRepository.GetAllAsync(cancellationToken);

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

        var dtos = customers
            .OrderBy(c => c.FullName)
            .Select(c => new CustomerDto(c.Id, c.FullName, c.Phone, c.Email, c.Level, c.BranchId, c.IsBlocked, c.CreatedAtUtc, c.MemberCode))
            .ToList();

        return PagedResult<CustomerDto>.Create(dtos, request.Page, request.PageSize);
    }
}
