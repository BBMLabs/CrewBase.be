using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Branches;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Instructors;

namespace RowingClub.Scheduling.Application.Panel;

/// <summary>Bir şubenin tüm detayı: şube bilgisi + o şubeye atanmış üye/tekne/eğitmenler ("şube paneli").</summary>
public sealed record GetBranchDetailQuery(Guid BranchId) : IRequest<BranchDetailDto>;

public sealed record BranchDetailDto(
    BranchDto Branch,
    List<CustomerDto> Members,
    List<BoatDto> Boats,
    List<InstructorDto> Instructors);

public sealed class GetBranchDetailQueryHandler(
    IBranchRepository branchRepository,
    ICustomerRepository customerRepository,
    IBoatRepository boatRepository,
    IInstructorRepository instructorRepository)
    : IRequestHandler<GetBranchDetailQuery, BranchDetailDto>
{
    public async Task<BranchDetailDto> Handle(GetBranchDetailQuery request, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken)
            ?? throw new NotFoundException("Branch", request.BranchId.ToString());

        var members = (await customerRepository.GetAllAsync(cancellationToken))
            .Where(c => c.BranchId == branch.Id)
            .OrderBy(c => c.FullName)
            .Select(c => new CustomerDto(c.Id, c.FullName, c.Phone, c.Email, c.Level, c.BranchId, c.IsBlocked, c.CreatedAtUtc, c.MemberCode))
            .ToList();

        var boats = (await boatRepository.GetAllAsync(cancellationToken))
            .Where(b => b.BranchId == branch.Id)
            .OrderBy(b => (int)b.Class).ThenBy(b => b.Name)
            .Select(b => new BoatDto(b.Id, b.Name, b.Class.Label(), b.Class.Capacity(), b.IsActive, b.BranchId))
            .ToList();

        var instructors = (await instructorRepository.GetAllAsync(cancellationToken))
            .Where(i => i.BranchId == branch.Id)
            .OrderBy(i => i.FullName)
            .Select(i => new InstructorDto(i.Id, i.FullName, i.Phone, i.Email, i.IsActive, i.BranchId))
            .ToList();

        return new BranchDetailDto(BranchMapper.ToDto(branch), members, boats, instructors);
    }
}
