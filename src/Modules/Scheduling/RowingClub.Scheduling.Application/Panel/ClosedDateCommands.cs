using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.Scheduling.Application.Panel;

/// <summary>Randevu tarih yönetimi: haftalık düzenin üstüne tekil kapalı günler (bayram, bakım...).</summary>
public sealed record GetClosedDatesQuery : IRequest<List<ClosedDateDto>>;

public sealed record ClosedDateDto(Guid Id, DateOnly Date, string? Reason);

public sealed record AddClosedDateCommand(DateOnly Date, string? Reason) : ICommand<ClosedDateDto>;

public sealed record RemoveClosedDateCommand(DateOnly Date) : ICommand<Unit>;

public sealed class GetClosedDatesQueryHandler(IClosedDateRepository repository)
    : IRequestHandler<GetClosedDatesQuery, List<ClosedDateDto>>
{
    public async Task<List<ClosedDateDto>> Handle(GetClosedDatesQuery request, CancellationToken cancellationToken)
    {
        var dates = await repository.GetFromAsync(
            DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-30), cancellationToken);
        return dates.OrderBy(d => d.Date).Select(d => new ClosedDateDto(d.Id, d.Date, d.Reason)).ToList();
    }
}

public sealed class AddClosedDateCommandHandler(
    IClosedDateRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<AddClosedDateCommand, ClosedDateDto>
{
    public async Task<ClosedDateDto> Handle(AddClosedDateCommand request, CancellationToken cancellationToken)
    {
        if (await repository.IsClosedAsync(request.Date, cancellationToken))
            throw new DomainException("already_closed", "Bu tarih zaten kapalı.");

        var closed = ClosedDate.Create(request.Date, request.Reason);
        repository.Add(closed);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ClosedDateDto(closed.Id, closed.Date, closed.Reason);
    }
}

public sealed class RemoveClosedDateCommandHandler(
    IClosedDateRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<RemoveClosedDateCommand, Unit>
{
    public async Task<Unit> Handle(RemoveClosedDateCommand request, CancellationToken cancellationToken)
    {
        var closed = await repository.GetByDateAsync(request.Date, cancellationToken)
            ?? throw new NotFoundException("ClosedDate", request.Date.ToString("yyyy-MM-dd"));

        repository.Remove(closed);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
