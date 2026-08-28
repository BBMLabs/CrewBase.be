using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Boats;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record BoatDto(Guid Id, string Name, string Class, int Capacity, bool IsActive, Guid? BranchId);

public sealed record GetBoatsQuery(string? Search = null, Guid? BranchId = null, bool? IsActive = null) : IRequest<List<BoatDto>>;

public sealed record CreateBoatCommand(string Name, string BoatClass, Guid? BranchId) : ICommand<BoatDto>;

public sealed record UpdateBoatCommand(Guid Id, string Name, string BoatClass, bool IsActive, Guid? BranchId)
    : ICommand<BoatDto>;

public sealed record DeleteBoatCommand(Guid Id) : ICommand<Unit>;

public sealed class GetBoatsQueryHandler(IBoatRepository repository)
    : IRequestHandler<GetBoatsQuery, List<BoatDto>>
{
    public async Task<List<BoatDto>> Handle(GetBoatsQuery request, CancellationToken cancellationToken)
    {
        var boats = await repository.GetAllAsync(cancellationToken);

        if (request.BranchId is { } branchId)
            boats = boats.Where(b => b.BranchId == branchId).ToList();

        if (request.IsActive is { } isActive)
            boats = boats.Where(b => b.IsActive == isActive).ToList();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            boats = boats.Where(b => b.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return boats
            .OrderBy(b => (int)b.Class).ThenBy(b => b.Name)
            .Select(BoatMapper.ToDto)
            .ToList();
    }
}

public sealed class CreateBoatCommandHandler(
    IBoatRepository repository, ISchedulingUnitOfWork unitOfWork, ITenantDatabase tenantDatabase)
    : IRequestHandler<CreateBoatCommand, BoatDto>
{
    public async Task<BoatDto> Handle(CreateBoatCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("invalid_name", "Tekne adı boş olamaz.");

        if (await repository.CountActiveAsync(cancellationToken) >= tenantDatabase.MaxBoats)
            throw new DomainException("boat_limit_reached",
                $"Tekne limitine ulaşıldı. Mevcut paketiniz en fazla {tenantDatabase.MaxBoats} tekneye izin verir; devam etmek için paketinizi yükseltin.");

        var boat = Boat.Create(request.Name, BoatClassExtensions.Parse(request.BoatClass), request.BranchId);
        repository.Add(boat);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return BoatMapper.ToDto(boat);
    }
}

public sealed class UpdateBoatCommandHandler(
    IBoatRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<UpdateBoatCommand, BoatDto>
{
    public async Task<BoatDto> Handle(UpdateBoatCommand request, CancellationToken cancellationToken)
    {
        var boat = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Boat", request.Id.ToString());

        boat.Update(request.Name, BoatClassExtensions.Parse(request.BoatClass), request.IsActive, request.BranchId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return BoatMapper.ToDto(boat);
    }
}

public sealed class DeleteBoatCommandHandler(
    IBoatRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<DeleteBoatCommand, Unit>
{
    public async Task<Unit> Handle(DeleteBoatCommand request, CancellationToken cancellationToken)
    {
        var boat = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Boat", request.Id.ToString());

        repository.Remove(boat);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

internal static class BoatMapper
{
    public static BoatDto ToDto(Boat boat) =>
        new(boat.Id, boat.Name, boat.Class.Label(), boat.Class.Capacity(), boat.IsActive, boat.BranchId);
}
