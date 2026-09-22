using MediatR;
using RowingClub.Scheduling.Domain.Branches;
using RowingClub.Scheduling.Domain.Customers;

namespace RowingClub.Scheduling.Application.PublicOptions;

/// <summary>Şubenin kendi tekil sitesi (/branch/{code}) için genel bilgileri.</summary>
public sealed record GetPublicBranchQuery(string Code) : IRequest<PublicBranchDto?>;

public sealed record PublicBranchDto(
    string Code, string Name, string? Address, string? Phone, string? Description, bool IsActive);

public sealed class GetPublicBranchQueryHandler(IBranchRepository branchRepository)
    : IRequestHandler<GetPublicBranchQuery, PublicBranchDto?>
{
    public async Task<PublicBranchDto?> Handle(GetPublicBranchQuery request, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (branch is null)
            return null;

        // Pasif şube "bulunamadı" değildir - genel site bunu görüp rezervasyon yerine
        // "bakımda" göstermeli (bkz. BranchSite.tsx). Aktif şube listesi (aşağıdaki
        // GetPublicBranchListQueryHandler) zaten pasif şubeleri filtreliyor.
        return new PublicBranchDto(branch.Code, branch.Name, branch.Address, branch.Phone, branch.Description, branch.IsActive);
    }
}

/// <summary>Kök alan sitesinde (/) şube seçtirmek için aktif şubelerin kısa listesi.</summary>
public sealed record GetPublicBranchListQuery : IRequest<List<PublicBranchSummaryDto>>;

public sealed record PublicBranchSummaryDto(string Code, string Name, string? Address);

public sealed class GetPublicBranchListQueryHandler(
    IBranchRepository branchRepository, ICustomerRepository customerRepository)
    : IRequestHandler<GetPublicBranchListQuery, List<PublicBranchSummaryDto>>
{
    public async Task<List<PublicBranchSummaryDto>> Handle(
        GetPublicBranchListQuery request, CancellationToken cancellationToken)
    {
        var branches = await branchRepository.GetAllAsync(cancellationToken);
        var memberCounts = await customerRepository.GetMemberCountsByBranchAsync(cancellationToken);

        return branches
            .Where(b => b.IsActive)
            .OrderByDescending(b => memberCounts.GetValueOrDefault(b.Id))
            .ThenBy(b => b.Name)
            .Select(b => new PublicBranchSummaryDto(b.Code, b.Name, b.Address))
            .ToList();
    }
}
