using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Application.Companies.SiteContent;

public sealed record GalleryImageDto(Guid Id, string ImagePath);

public sealed record CompanySiteContentDto(
    string? LogoPath, string Tagline, string AboutText, string? InstagramUrl, string? FacebookUrl,
    string? YoutubeUrl, string? LinkedinUrl, string? XUrl, string? WhatsappUrl, string? TelegramUrl,
    string? PinterestUrl, string? GoogleMapsUrl, List<GalleryImageDto> GalleryImages);

public sealed record GetCompanySiteContentQuery(Guid CompanyId) : IQuery<CompanySiteContentDto>;

public sealed record UpdateCompanySiteContentCommand(
    Guid CompanyId, string Tagline, string AboutText, string? InstagramUrl, string? FacebookUrl,
    string? YoutubeUrl, string? LinkedinUrl, string? XUrl, string? WhatsappUrl, string? TelegramUrl,
    string? PinterestUrl, string? GoogleMapsUrl) : ICommand<CompanySiteContentDto>;

public sealed record SetCompanyLogoCommand(Guid CompanyId, string LogoPath) : ICommand<CompanySiteContentDto>;

public sealed record AddCompanyGalleryImageCommand(Guid CompanyId, string ImagePath) : ICommand<CompanySiteContentDto>;

public sealed record RemoveCompanyGalleryImageCommand(Guid CompanyId, Guid ImageId) : ICommand<string>;

public sealed class GetCompanySiteContentQueryHandler(
    ICompanyRepository companyRepository, ICompanyGalleryImageRepository galleryRepository)
    : IRequestHandler<GetCompanySiteContentQuery, CompanySiteContentDto>
{
    public async Task<CompanySiteContentDto> Handle(GetCompanySiteContentQuery request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new NotFoundException("Company", request.CompanyId.ToString());

        var images = await galleryRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);

        return ToDto(company, images);
    }

    internal static CompanySiteContentDto ToDto(Company company, List<CompanyGalleryImage> images) => new(
        company.LogoPath, company.Tagline, company.AboutText, company.InstagramUrl, company.FacebookUrl,
        company.YoutubeUrl, company.LinkedinUrl, company.XUrl, company.WhatsappUrl, company.TelegramUrl,
        company.PinterestUrl, company.GoogleMapsUrl, images.Select(i => new GalleryImageDto(i.Id, i.ImagePath)).ToList());
}

public sealed class UpdateCompanySiteContentCommandHandler(
    ICompanyRepository companyRepository, ICompanyGalleryImageRepository galleryRepository)
    : IRequestHandler<UpdateCompanySiteContentCommand, CompanySiteContentDto>
{
    public async Task<CompanySiteContentDto> Handle(UpdateCompanySiteContentCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new NotFoundException("Company", request.CompanyId.ToString());

        company.UpdateSiteContent(request.Tagline, request.AboutText);
        company.UpdateSocialLinks(
            request.InstagramUrl, request.FacebookUrl, request.YoutubeUrl, request.LinkedinUrl,
            request.XUrl, request.WhatsappUrl, request.TelegramUrl, request.PinterestUrl);
        company.UpdateGoogleMapsUrl(request.GoogleMapsUrl);
        companyRepository.Update(company);

        var images = await galleryRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
        return GetCompanySiteContentQueryHandler.ToDto(company, images);
    }
}

public sealed class SetCompanyLogoCommandHandler(
    ICompanyRepository companyRepository, ICompanyGalleryImageRepository galleryRepository)
    : IRequestHandler<SetCompanyLogoCommand, CompanySiteContentDto>
{
    public async Task<CompanySiteContentDto> Handle(SetCompanyLogoCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new NotFoundException("Company", request.CompanyId.ToString());

        company.SetLogo(request.LogoPath);
        companyRepository.Update(company);

        var images = await galleryRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
        return GetCompanySiteContentQueryHandler.ToDto(company, images);
    }
}

public sealed class AddCompanyGalleryImageCommandHandler(
    ICompanyRepository companyRepository, ICompanyGalleryImageRepository galleryRepository)
    : IRequestHandler<AddCompanyGalleryImageCommand, CompanySiteContentDto>
{
    public async Task<CompanySiteContentDto> Handle(AddCompanyGalleryImageCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new NotFoundException("Company", request.CompanyId.ToString());

        var existingCount = await galleryRepository.CountByCompanyIdAsync(request.CompanyId, cancellationToken);
        if (existingCount >= CompanyGalleryImage.MaxImagesPerCompany)
            throw new DomainException(
                "gallery_limit_reached",
                $"Galeriye en fazla {CompanyGalleryImage.MaxImagesPerCompany} görsel eklenebilir.");

        galleryRepository.Add(CompanyGalleryImage.Create(request.CompanyId, request.ImagePath));

        var images = await galleryRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
        return GetCompanySiteContentQueryHandler.ToDto(company, images);
    }
}

public sealed class RemoveCompanyGalleryImageCommandHandler(ICompanyGalleryImageRepository galleryRepository)
    : IRequestHandler<RemoveCompanyGalleryImageCommand, string>
{
    public async Task<string> Handle(RemoveCompanyGalleryImageCommand request, CancellationToken cancellationToken)
    {
        var image = await galleryRepository.GetByIdAsync(request.ImageId, cancellationToken);
        if (image is null || image.CompanyId != request.CompanyId)
            throw new NotFoundException("CompanyGalleryImage", request.ImageId.ToString());

        galleryRepository.Remove(image);

        return image.ImagePath;
    }
}
