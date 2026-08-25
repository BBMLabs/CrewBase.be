using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Boats;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record BoatDto(Guid Id, string Name, string Class, int Capacity, bool IsActive, Guid? BranchId);

public sealed record GetBoatsQuery : IRequest<List<BoatDto>>;

public sealed record CreateBoatCommand(string Name, string BoatClass, Guid? BranchId) : ICommand<BoatDto>;

public sealed record UpdateBoatCommand(Guid Id, string Name, string BoatClass, bool IsActive, Guid? BranchId)
    : ICommand<BoatDto>;

public sealed class GetBoatsQueryHandler(IBoatRepository repository)
    : IRequestHandler<GetBoatsQuery, List<BoatDto>>
{
    public async Task<List<BoatDto>> Handle(GetBoatsQuery request, CancellationToken cancellationToken)
    {
        var boats = await repository.GetAllAsync(cancellationToken);
        return boats
            .OrderBy(b => (int)b.Class).ThenBy(b => b.Name)
            .Select(BoatMapper.ToDto)
            .ToList();
    }
}

public sealed class CreateBoatCommandHandler(
    IBoatRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<CreateBoatCommand, BoatDto>
{
    public async Task<BoatDto> Handle(CreateBoatCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("invalid_name", "Tekne adı boş olamaz.");

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

internal static class BoatMapper
{
    public static BoatDto ToDto(Boat boat) =>
        new(boat.Id, boat.Name, boat.Class.Label(), boat.Class.Capacity(), boat.IsActive, boat.BranchId);
}
