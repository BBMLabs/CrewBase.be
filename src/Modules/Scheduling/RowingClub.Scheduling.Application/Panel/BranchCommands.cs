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

public sealed record GetBranchesQuery(string? Search = null, bool? IsActive = null, string? Cursor = null, int Limit = 25)
    : IRequest<KeysetResult<BranchDto>>;

public sealed record CreateBranchCommand(
    string Name, string? Address, string? Phone,
    string? ManagerName, string? ManagerPhone, string? ManagerEmail,
    string? TaxNumber, string? Description) : ICommand<BranchDto>;

public sealed record UpdateBranchCommand(
    Guid Id, string Name, string? Address, string? Phone, bool IsActive,
    string? ManagerName, string? ManagerPhone, string? ManagerEmail,
    string? TaxNumber, string? Description) : ICommand<BranchDto>;

public sealed class GetBranchesQueryHandler(IBranchRepository repository)
    : IRequestHandler<GetBranchesQuery, KeysetResult<BranchDto>>
{
    public async Task<KeysetResult<BranchDto>> Handle(GetBranchesQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 200);
        var hasCursor = KeysetCursor.TryDecode(request.Cursor, 1, out var keyParts, out var cursorId);

        var branches = await repository.GetPageAsync(
            request.Search, request.IsActive, hasCursor ? keyParts[0] : null, hasCursor ? cursorId : null,
            limit + 1, cancellationToken);

        var (page, nextCursor) = KeysetPage.Trim(branches, limit, b => b.Id, b => [b.Name]);

        var totalCount = await repository.CountAsync(request.Search, request.IsActive, cancellationToken);

        return new KeysetResult<BranchDto>(page.Select(BranchMapper.ToDto).ToList(), nextCursor, totalCount);
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
