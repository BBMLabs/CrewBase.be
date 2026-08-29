using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Instructors;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record InstructorDto(Guid Id, string FullName, string? Phone, string? Email, bool IsActive, Guid? BranchId);

public sealed record GetInstructorsQuery(
    string? Search = null, Guid? BranchId = null, bool? IsActive = null, int Page = 1, int PageSize = 25)
    : IRequest<PagedResult<InstructorDto>>;

public sealed record CreateInstructorCommand(string FullName, string? Phone, string? Email, Guid? BranchId)
    : ICommand<InstructorDto>;

public sealed record UpdateInstructorCommand(
    Guid Id, string FullName, string? Phone, string? Email, bool IsActive, Guid? BranchId)
    : ICommand<InstructorDto>;

public sealed record DeleteInstructorCommand(Guid Id) : ICommand<Unit>;

public sealed class GetInstructorsQueryHandler(IInstructorRepository repository)
    : IRequestHandler<GetInstructorsQuery, PagedResult<InstructorDto>>
{
    public async Task<PagedResult<InstructorDto>> Handle(GetInstructorsQuery request, CancellationToken cancellationToken)
    {
        var instructors = await repository.GetAllAsync(cancellationToken);

        if (request.BranchId is { } branchId)
            instructors = instructors.Where(i => i.BranchId == branchId).ToList();

        if (request.IsActive is { } isActive)
            instructors = instructors.Where(i => i.IsActive == isActive).ToList();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            instructors = instructors.Where(i =>
                i.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (i.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (i.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        var dtos = instructors
            .OrderBy(i => i.FullName)
            .Select(i => new InstructorDto(i.Id, i.FullName, i.Phone, i.Email, i.IsActive, i.BranchId))
            .ToList();

        return PagedResult<InstructorDto>.Create(dtos, request.Page, request.PageSize);
    }
}

public sealed class CreateInstructorCommandHandler(
    IInstructorRepository repository, ISchedulingUnitOfWork unitOfWork, ITenantDatabase tenantDatabase)
    : IRequestHandler<CreateInstructorCommand, InstructorDto>
{
    public async Task<InstructorDto> Handle(CreateInstructorCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new DomainException("invalid_name", "Eğitmen adı boş olamaz.");

        if (await repository.CountActiveAsync(cancellationToken) >= tenantDatabase.MaxInstructors)
            throw new DomainException("instructor_limit_reached",
                $"Eğitmen limitine ulaşıldı. Mevcut paketiniz en fazla {tenantDatabase.MaxInstructors} eğitmene izin verir; devam etmek için paketinizi yükseltin.");

        var instructor = Instructor.Create(request.FullName, request.Phone, request.Email, request.BranchId);
        repository.Add(instructor);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new InstructorDto(
            instructor.Id, instructor.FullName, instructor.Phone, instructor.Email, instructor.IsActive, instructor.BranchId);
    }
}

public sealed class UpdateInstructorCommandHandler(
    IInstructorRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<UpdateInstructorCommand, InstructorDto>
{
    public async Task<InstructorDto> Handle(UpdateInstructorCommand request, CancellationToken cancellationToken)
    {
        var instructor = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Instructor", request.Id.ToString());

        instructor.Update(request.FullName, request.Phone, request.Email, request.IsActive, request.BranchId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new InstructorDto(
            instructor.Id, instructor.FullName, instructor.Phone, instructor.Email, instructor.IsActive, instructor.BranchId);
    }
}

public sealed class DeleteInstructorCommandHandler(
    IInstructorRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<DeleteInstructorCommand, Unit>
{
    public async Task<Unit> Handle(DeleteInstructorCommand request, CancellationToken cancellationToken)
    {
        var instructor = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Instructor", request.Id.ToString());

        repository.Remove(instructor);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
