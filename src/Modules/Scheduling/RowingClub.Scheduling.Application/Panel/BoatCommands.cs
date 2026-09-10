using System.Globalization;
using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Boats;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record BoatDto(Guid Id, string Name, string Class, int Capacity, bool IsActive, Guid? BranchId);

public sealed record GetBoatsQuery(
    string? Search = null, Guid? BranchId = null, bool? IsActive = null, string? Cursor = null, int Limit = 25)
    : IRequest<KeysetResult<BoatDto>>;

public sealed record CreateBoatCommand(string Name, string BoatClass, Guid? BranchId) : ICommand<BoatDto>;

public sealed record UpdateBoatCommand(Guid Id, string Name, string BoatClass, bool IsActive, Guid? BranchId)
    : ICommand<BoatDto>;

public sealed record DeleteBoatCommand(Guid Id) : ICommand<Unit>;

public sealed class GetBoatsQueryHandler(IBoatRepository repository)
    : IRequestHandler<GetBoatsQuery, KeysetResult<BoatDto>>
{
    public async Task<KeysetResult<BoatDto>> Handle(GetBoatsQuery request, CancellationToken cancellationToken)
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

        var ordered = boats.OrderBy(b => (int)b.Class).ThenBy(b => b.Name).ThenBy(b => b.Id).ToList();

        var limit = Math.Clamp(request.Limit, 1, 200);
        var hasCursor = KeysetCursor.TryDecode(request.Cursor, 2, out var keyParts, out var cursorId);
        var candidates = hasCursor
            ? KeysetPage.SliceAfterCursor(
                ordered,
                b => IsAfterCursor(b, int.Parse(keyParts[0], CultureInfo.InvariantCulture), keyParts[1], cursorId),
                limit + 1)
            : ordered.Take(limit + 1).ToList();

        var (page, nextCursor) = KeysetPage.Trim(
            candidates, limit, b => b.Id, b => [((int)b.Class).ToString(CultureInfo.InvariantCulture), b.Name]);

        return new KeysetResult<BoatDto>(page.Select(BoatMapper.ToDto).ToList(), nextCursor, ordered.Count);
    }

    private static bool IsAfterCursor(Boat boat, int cursorClass, string cursorName, Guid cursorId)
    {
        var classCompare = ((int)boat.Class).CompareTo(cursorClass);
        if (classCompare != 0) return classCompare > 0;
        var nameCompare = string.Compare(boat.Name, cursorName);
        if (nameCompare != 0) return nameCompare > 0;
        return boat.Id.CompareTo(cursorId) > 0;
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
