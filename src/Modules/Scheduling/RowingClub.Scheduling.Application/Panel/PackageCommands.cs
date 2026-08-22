using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Packages;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record PackageDto(
    Guid Id, string Name, string? Description, int SessionCount, decimal Price, bool IsActive);

public sealed record GetPackagesQuery : IRequest<List<PackageDto>>;

public sealed record CreatePackageCommand(string Name, string? Description, int SessionCount, decimal Price)
    : ICommand<PackageDto>;

public sealed record UpdatePackageCommand(
    Guid Id, string Name, string? Description, int SessionCount, decimal Price, bool IsActive)
    : ICommand<PackageDto>;

public sealed class GetPackagesQueryHandler(ILessonPackageRepository repository)
    : IRequestHandler<GetPackagesQuery, List<PackageDto>>
{
    public async Task<List<PackageDto>> Handle(GetPackagesQuery request, CancellationToken cancellationToken)
    {
        var packages = await repository.GetAllAsync(cancellationToken);
        return packages
            .OrderBy(p => p.Price)
            .Select(p => new PackageDto(p.Id, p.Name, p.Description, p.SessionCount, p.Price, p.IsActive))
            .ToList();
    }
}

public sealed class CreatePackageCommandHandler(
    ILessonPackageRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<CreatePackageCommand, PackageDto>
{
    public async Task<PackageDto> Handle(CreatePackageCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("invalid_name", "Paket adı boş olamaz.");

        var package = LessonPackage.Create(request.Name, request.Description, request.SessionCount, request.Price);
        repository.Add(package);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PackageDto(package.Id, package.Name, package.Description, package.SessionCount, package.Price, package.IsActive);
    }
}

public sealed class UpdatePackageCommandHandler(
    ILessonPackageRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePackageCommand, PackageDto>
{
    public async Task<PackageDto> Handle(UpdatePackageCommand request, CancellationToken cancellationToken)
    {
        var package = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("LessonPackage", request.Id.ToString());

        package.Update(request.Name, request.Description, request.SessionCount, request.Price, request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PackageDto(package.Id, package.Name, package.Description, package.SessionCount, package.Price, package.IsActive);
    }
}
