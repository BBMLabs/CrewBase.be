using MediatR;
using RowingClub.Scheduling.Domain.Packages;

namespace RowingClub.Scheduling.Application.Members;

public sealed record GetMemberPackagesQuery(Guid CustomerId) : IRequest<List<CustomerPackageDto>>;

public sealed record CustomerPackageDto(
    Guid Id,
    Guid CustomerId,
    string PackageName,
    int TotalSessions,
    int RemainingSessions,
    DateTimeOffset AssignedAtUtc);

public sealed class GetMemberPackagesQueryHandler(ICustomerPackageRepository repository)
    : IRequestHandler<GetMemberPackagesQuery, List<CustomerPackageDto>>
{
    public async Task<List<CustomerPackageDto>> Handle(
        GetMemberPackagesQuery request, CancellationToken cancellationToken)
    {
        var packages = await repository.GetByCustomerAsync(request.CustomerId, cancellationToken);
        return packages
            .OrderByDescending(p => p.AssignedAtUtc)
            .Select(p => new CustomerPackageDto(
                p.Id, p.CustomerId, p.PackageName, p.TotalSessions, p.RemainingSessions, p.AssignedAtUtc))
            .ToList();
    }
}
