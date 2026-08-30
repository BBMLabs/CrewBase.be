using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Application.Files;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Packages;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record PackageDto(
    Guid Id, string Name, string? Description, int SessionCount, decimal Price, bool IsActive,
    string? ImagePath, int? ValidityDays);

public sealed record GetPackagesQuery(string? Search = null, int Page = 1, int PageSize = 25)
    : IRequest<PagedResult<PackageDto>>;

public sealed record CreatePackageCommand(
    string Name, string? Description, int SessionCount, decimal Price, int? ValidityDays)
    : ICommand<PackageDto>;

public sealed record UpdatePackageCommand(
    Guid Id, string Name, string? Description, int SessionCount, decimal Price, bool IsActive, int? ValidityDays)
    : ICommand<PackageDto>;

/// <summary>Görsel yüklendikten sonra (bkz. IFileStorageService) yolunu pakete yazar.</summary>
public sealed record SetPackageImageCommand(Guid PackageId, string ImagePath) : ICommand<PackageDto>;

/// <summary>
/// Paketi kalıcı olarak siler (hard delete) ve varsa görselini depodan (R2/yerel disk) temizler.
/// Bu paketten en az bir CustomerPackage türetilmişse (satın alınmış/atanmış) engellenir - aksi
/// halde customer_packages.LessonPackageId FK'sı (Restrict) veritabanı hatası verir VE daha
/// önemlisi, o üyelerin halihazırda ödedikleri/sahip oldukları ders bakiyesi sessizce kaybolurdu
/// (veri bütünlüğü, işlevsellikten önce gelir - bkz. CLAUDE.md öncelik sırası).
/// </summary>
public sealed record DeletePackageCommand(Guid Id) : ICommand<Unit>;

public sealed class GetPackagesQueryHandler(ILessonPackageRepository repository)
    : IRequestHandler<GetPackagesQuery, PagedResult<PackageDto>>
{
    public async Task<PagedResult<PackageDto>> Handle(GetPackagesQuery request, CancellationToken cancellationToken)
    {
        var packages = await repository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            packages = packages.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var dtos = packages
            .OrderBy(p => p.Price)
            .Select(ToDto)
            .ToList();

        return PagedResult<PackageDto>.Create(dtos, request.Page, request.PageSize);
    }

    internal static PackageDto ToDto(LessonPackage p) => new(
        p.Id, p.Name, p.Description, p.SessionCount, p.Price, p.IsActive, p.ImagePath, p.ValidityDays);
}

public sealed class CreatePackageCommandHandler(
    ILessonPackageRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<CreatePackageCommand, PackageDto>
{
    public async Task<PackageDto> Handle(CreatePackageCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("invalid_name", "Paket adı boş olamaz.");

        var package = LessonPackage.Create(
            request.Name, request.Description, request.SessionCount, request.Price, request.ValidityDays);
        repository.Add(package);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return GetPackagesQueryHandler.ToDto(package);
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

        package.Update(
            request.Name, request.Description, request.SessionCount, request.Price, request.IsActive,
            request.ValidityDays);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return GetPackagesQueryHandler.ToDto(package);
    }
}

public sealed class SetPackageImageCommandHandler(
    ILessonPackageRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<SetPackageImageCommand, PackageDto>
{
    public async Task<PackageDto> Handle(SetPackageImageCommand request, CancellationToken cancellationToken)
    {
        var package = await repository.GetByIdAsync(request.PackageId, cancellationToken)
            ?? throw new NotFoundException("LessonPackage", request.PackageId.ToString());

        package.SetImage(request.ImagePath);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return GetPackagesQueryHandler.ToDto(package);
    }
}

public sealed class DeletePackageCommandHandler(
    ILessonPackageRepository repository,
    ICustomerPackageRepository customerPackageRepository,
    IFileStorageService fileStorage,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<DeletePackageCommand, Unit>
{
    public async Task<Unit> Handle(DeletePackageCommand request, CancellationToken cancellationToken)
    {
        var package = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("LessonPackage", request.Id.ToString());

        var hasPurchases = (await customerPackageRepository.GetAllAsync(cancellationToken))
            .Any(cp => cp.LessonPackageId == package.Id);
        if (hasPurchases)
            throw new DomainException("package_has_purchases",
                "Bu paket üyelere satın alınmış/atanmış; silinemez. Önce pasife alabilirsiniz.");

        if (package.ImagePath is { } imagePath)
            await fileStorage.DeleteAsync(imagePath, cancellationToken);

        repository.Remove(package);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
