using MediatR;
using RowingClub.Scheduling.Domain.Branches;

namespace RowingClub.Scheduling.Application.PublicOptions;

/// <summary>Şubenin kendi tekil sitesi (/sube/{code}) için genel bilgileri.</summary>
public sealed record GetPublicBranchQuery(string Code) : IRequest<PublicBranchDto?>;

public sealed record PublicBranchDto(
    string Code, string Name, string? Address, string? Phone, string? Description);

public sealed class GetPublicBranchQueryHandler(IBranchRepository branchRepository)
    : IRequestHandler<GetPublicBranchQuery, PublicBranchDto?>
{
    public async Task<PublicBranchDto?> Handle(GetPublicBranchQuery request, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (branch is null || !branch.IsActive)
            return null;

        return new PublicBranchDto(branch.Code, branch.Name, branch.Address, branch.Phone, branch.Description);
    }
}

/// <summary>Kök alan sitesinde (/) şube seçtirmek için aktif şubelerin kısa listesi.</summary>
public sealed record GetPublicBranchListQuery : IRequest<List<PublicBranchSummaryDto>>;

public sealed record PublicBranchSummaryDto(string Code, string Name, string? Address);

public sealed class GetPublicBranchListQueryHandler(IBranchRepository branchRepository)
    : IRequestHandler<GetPublicBranchListQuery, List<PublicBranchSummaryDto>>
{
    public async Task<List<PublicBranchSummaryDto>> Handle(
        GetPublicBranchListQuery request, CancellationToken cancellationToken)
    {
        var branches = await branchRepository.GetAllAsync(cancellationToken);
        return branches
            .Where(b => b.IsActive)
            .Select(b => new PublicBranchSummaryDto(b.Code, b.Name, b.Address))
            .ToList();
    }
}
