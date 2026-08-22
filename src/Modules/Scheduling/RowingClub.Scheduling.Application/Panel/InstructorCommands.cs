using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Instructors;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record InstructorDto(Guid Id, string FullName, string? Phone, string? Email, bool IsActive);

public sealed record GetInstructorsQuery : IRequest<List<InstructorDto>>;

public sealed record CreateInstructorCommand(string FullName, string? Phone, string? Email)
    : ICommand<InstructorDto>;

public sealed record UpdateInstructorCommand(Guid Id, string FullName, string? Phone, string? Email, bool IsActive)
    : ICommand<InstructorDto>;

public sealed class GetInstructorsQueryHandler(IInstructorRepository repository)
    : IRequestHandler<GetInstructorsQuery, List<InstructorDto>>
{
    public async Task<List<InstructorDto>> Handle(GetInstructorsQuery request, CancellationToken cancellationToken)
    {
        var instructors = await repository.GetAllAsync(cancellationToken);
        return instructors
            .OrderBy(i => i.FullName)
            .Select(i => new InstructorDto(i.Id, i.FullName, i.Phone, i.Email, i.IsActive))
            .ToList();
    }
}

public sealed class CreateInstructorCommandHandler(
    IInstructorRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<CreateInstructorCommand, InstructorDto>
{
    public async Task<InstructorDto> Handle(CreateInstructorCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new DomainException("invalid_name", "Eğitmen adı boş olamaz.");

        var instructor = Instructor.Create(request.FullName, request.Phone, request.Email);
        repository.Add(instructor);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new InstructorDto(instructor.Id, instructor.FullName, instructor.Phone, instructor.Email, instructor.IsActive);
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

        instructor.Update(request.FullName, request.Phone, request.Email, request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new InstructorDto(instructor.Id, instructor.FullName, instructor.Phone, instructor.Email, instructor.IsActive);
    }
}
