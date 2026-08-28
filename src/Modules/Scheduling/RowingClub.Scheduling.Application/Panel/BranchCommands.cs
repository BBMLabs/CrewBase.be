using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Branches;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record BranchDto(
    Guid Id, string Code, string Name, string? Address, string? Phone, bool IsActive,
    string? ManagerName, string? ManagerPhone, string? ManagerEmail,
    string? TaxNumber, string? Description, string? LogoPath);

public sealed record GetBranchesQuery(string? Search = null, bool? IsActive = null) : IRequest<List<BranchDto>>;

public sealed record CreateBranchCommand(
    string Name, string? Address, string? Phone,
    string? ManagerName, string? ManagerPhone, string? ManagerEmail,
    string? TaxNumber, string? Description) : ICommand<BranchDto>;

public sealed record UpdateBranchCommand(
    Guid Id, string Name, string? Address, string? Phone, bool IsActive,
    string? ManagerName, string? ManagerPhone, string? ManagerEmail,
    string? TaxNumber, string? Description) : ICommand<BranchDto>;

public sealed class GetBranchesQueryHandler(IBranchRepository repository)
    : IRequestHandler<GetBranchesQuery, List<BranchDto>>
{
    public async Task<List<BranchDto>> Handle(GetBranchesQuery request, CancellationToken cancellationToken)
    {
        var branches = await repository.GetAllAsync(cancellationToken);

        if (request.IsActive is { } isActive)
            branches = branches.Where(b => b.IsActive == isActive).ToList();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            branches = branches.Where(b =>
                b.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (b.Address?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (b.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (b.ManagerName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        return branches
            .OrderBy(b => b.Name)
            .Select(BranchMapper.ToDto)
            .ToList();
    }
}

public sealed class CreateBranchCommandHandler(
    IBranchRepository repository, ISchedulingUnitOfWork unitOfWork, ITenantDatabase tenantDatabase)
    : IRequestHandler<CreateBranchCommand, BranchDto>
{
    public async Task<BranchDto> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("invalid_name", "Şube adı boş olamaz.");

        if (await repository.CountActiveAsync(cancellationToken) >= tenantDatabase.MaxBranches)
            throw new DomainException("branch_limit_reached",
                $"Şube limitine ulaşıldı. Mevcut paketiniz en fazla {tenantDatabase.MaxBranches} şubeye izin verir; devam etmek için paketinizi yükseltin.");

        var code = await GenerateUniqueCodeAsync(cancellationToken);
        var branch = Branch.Create(
            code, request.Name, request.Address, request.Phone,
            request.ManagerName, request.ManagerPhone, request.ManagerEmail,
            request.TaxNumber, request.Description);
        repository.Add(branch);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return BranchMapper.ToDto(branch);
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 10; i++)
        {
            var code = BranchCodeGenerator.Generate();
            if (!await repository.ExistsByCodeAsync(code, cancellationToken))
                return code;
        }

        throw new DomainException("code_generation_failed", "Şube kodu üretilemedi; tekrar deneyin.");
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

        branch.Update(
            request.Name, request.Address, request.Phone, request.IsActive,
            request.ManagerName, request.ManagerPhone, request.ManagerEmail,
            request.TaxNumber, request.Description);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return BranchMapper.ToDto(branch);
    }
}

public sealed record SetBranchLogoCommand(Guid BranchId, string LogoPath) : ICommand<BranchDto>;

public sealed class SetBranchLogoCommandHandler(
    IBranchRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<SetBranchLogoCommand, BranchDto>
{
    public async Task<BranchDto> Handle(SetBranchLogoCommand request, CancellationToken cancellationToken)
    {
        var branch = await repository.GetByIdAsync(request.BranchId, cancellationToken)
            ?? throw new NotFoundException("Branch", request.BranchId.ToString());

        branch.SetLogo(request.LogoPath);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return BranchMapper.ToDto(branch);
    }
}

internal static class BranchMapper
{
    public static BranchDto ToDto(Branch branch) =>
        new(branch.Id, branch.Code, branch.Name, branch.Address, branch.Phone, branch.IsActive,
            branch.ManagerName, branch.ManagerPhone, branch.ManagerEmail,
            branch.TaxNumber, branch.Description, branch.LogoPath);
}
