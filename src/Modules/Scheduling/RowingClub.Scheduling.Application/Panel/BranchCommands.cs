using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Branches;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record BranchDto(Guid Id, string Name, string? Address, string? Phone, bool IsActive);

public sealed record GetBranchesQuery : IRequest<List<BranchDto>>;

public sealed record CreateBranchCommand(string Name, string? Address, string? Phone) : ICommand<BranchDto>;

public sealed record UpdateBranchCommand(Guid Id, string Name, string? Address, string? Phone, bool IsActive)
    : ICommand<BranchDto>;

public sealed class GetBranchesQueryHandler(IBranchRepository repository)
    : IRequestHandler<GetBranchesQuery, List<BranchDto>>
{
    public async Task<List<BranchDto>> Handle(GetBranchesQuery request, CancellationToken cancellationToken)
    {
        var branches = await repository.GetAllAsync(cancellationToken);
        return branches
            .OrderBy(b => b.Name)
            .Select(BranchMapper.ToDto)
            .ToList();
    }
}

public sealed class CreateBranchCommandHandler(
    IBranchRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<CreateBranchCommand, BranchDto>
{
    public async Task<BranchDto> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("invalid_name", "Şube adı boş olamaz.");

        var branch = Branch.Create(request.Name, request.Address, request.Phone);
        repository.Add(branch);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return BranchMapper.ToDto(branch);
    }
}

public sealed class UpdateBranchCommandHandler(
    IBranchRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<UpdateBranchCommand, BranchDto>
{
    public async Task<BranchDto> Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Branch", request.Id.ToString());

        branch.Update(request.Name, request.Address, request.Phone, request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return BranchMapper.ToDto(branch);
    }
}

internal static class BranchMapper
{
    public static BranchDto ToDto(Branch branch) =>
        new(branch.Id, branch.Name, branch.Address, branch.Phone, branch.IsActive);
}
