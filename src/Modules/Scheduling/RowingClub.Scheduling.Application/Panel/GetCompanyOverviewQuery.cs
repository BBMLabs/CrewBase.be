using MediatR;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Branches;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Instructors;

namespace RowingClub.Scheduling.Application.Panel;

/// <summary>Platform panelinden bir firmanın özeti: sayılar + her şubenin kendi tekil kodu.</summary>
public sealed record GetCompanyOverviewQuery : IRequest<CompanyOverviewDto>;

public sealed record BranchOverviewDto(Guid Id, string Code, string Name, bool IsActive);

public sealed record CompanyOverviewDto(
    int MemberCount, int InstructorCount, int BoatCount, int BranchCount, List<BranchOverviewDto> Branches);

public sealed class GetCompanyOverviewQueryHandler(
    IBranchRepository branchRepository,
    ICustomerRepository customerRepository,
    IBoatRepository boatRepository,
    IInstructorRepository instructorRepository)
    : IRequestHandler<GetCompanyOverviewQuery, CompanyOverviewDto>
{
    public async Task<CompanyOverviewDto> Handle(GetCompanyOverviewQuery request, CancellationToken cancellationToken)
    {
        var branches = await branchRepository.GetAllAsync(cancellationToken);
        var memberCount = await customerRepository.CountAsync(cancellationToken);
        var boats = await boatRepository.GetAllAsync(cancellationToken);
        var instructors = await instructorRepository.GetAllAsync(cancellationToken);

        return new CompanyOverviewDto(
            memberCount,
            instructors.Count,
            boats.Count,
            branches.Count,
            branches
                .OrderBy(b => b.Name)
                .Select(b => new BranchOverviewDto(b.Id, b.Code, b.Name, b.IsActive))
                .ToList());
    }
}
