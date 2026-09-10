using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Scheduling.Domain.Customers;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record GetCustomersQuery(
    string? Search = null, Guid? BranchId = null, string? Cursor = null, int Limit = 25)
    : IRequest<KeysetResult<CustomerDto>>;

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
    : IRequestHandler<GetCustomersQuery, KeysetResult<CustomerDto>>
{
    public async Task<KeysetResult<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
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

        var ordered = customers.OrderBy(c => c.FullName).ThenBy(c => c.Id).ToList();

        var limit = Math.Clamp(request.Limit, 1, 200);
        var hasCursor = KeysetCursor.TryDecode(request.Cursor, 1, out var keyParts, out var cursorId);
        var candidates = hasCursor
            ? KeysetPage.SliceAfterCursor(ordered, c => IsAfterCursor(c, keyParts[0], cursorId), limit + 1)
            : ordered.Take(limit + 1).ToList();

        var (page, nextCursor) = KeysetPage.Trim(candidates, limit, c => c.Id, c => [c.FullName]);

        return new KeysetResult<CustomerDto>(
            page.Select(c => new CustomerDto(c.Id, c.FullName, c.Phone, c.Email, c.Level, c.BranchId, c.IsBlocked, c.CreatedAtUtc, c.MemberCode)).ToList(),
            nextCursor, ordered.Count);
    }

    private static bool IsAfterCursor(Customer customer, string cursorFullName, Guid cursorId)
    {
        var nameCompare = string.Compare(customer.FullName, cursorFullName);
        if (nameCompare != 0) return nameCompare > 0;
        return customer.Id.CompareTo(cursorId) > 0;
    }
}
