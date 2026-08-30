using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Campaigns;
using RowingClub.Scheduling.Domain.Packages;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record CampaignDto(
    Guid Id, Guid LessonPackageId, string PackageName, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc,
    decimal Price, int? MinLevel, int? MaxLevel, string Status, int ParticipantCount, decimal TotalRevenue);

public sealed record GetCampaignsQuery : IRequest<List<CampaignDto>>;

public sealed record CreateCampaignCommand(
    Guid LessonPackageId, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, decimal Price,
    int? MinLevel, int? MaxLevel)
    : ICommand<CampaignDto>;

public sealed record UpdateCampaignCommand(
    Guid Id, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, decimal Price, int? MinLevel, int? MaxLevel)
    : ICommand<CampaignDto>;

public sealed record DeleteCampaignCommand(Guid Id) : ICommand<Unit>;

internal static class CampaignStatus
{
    public static string For(Campaign campaign, DateTimeOffset now) =>
        now < campaign.StartsAtUtc ? "Pending" : now > campaign.EndsAtUtc ? "Ended" : "Active";
}

public sealed class GetCampaignsQueryHandler(
    ICampaignRepository campaignRepository,
    ILessonPackageRepository packageRepository,
    ICustomerPackageRepository customerPackageRepository)
    : IRequestHandler<GetCampaignsQuery, List<CampaignDto>>
{
    public async Task<List<CampaignDto>> Handle(GetCampaignsQuery request, CancellationToken cancellationToken)
    {
        var campaigns = await campaignRepository.GetAllAsync(cancellationToken);
        var packages = await packageRepository.GetAllAsync(cancellationToken);
        var packageNames = packages.ToDictionary(p => p.Id, p => p.Name);
        var customerPackages = await customerPackageRepository.GetAllAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        return campaigns
            .OrderByDescending(c => c.StartsAtUtc)
            .Select(c =>
            {
                var participants = customerPackages.Where(cp => cp.CampaignId == c.Id).ToList();
                return new CampaignDto(
                    c.Id, c.LessonPackageId, packageNames.GetValueOrDefault(c.LessonPackageId, "—"),
                    c.StartsAtUtc, c.EndsAtUtc, c.Price, c.MinLevel, c.MaxLevel, CampaignStatus.For(c, now),
                    participants.Count, participants.Sum(p => p.PricePaid ?? 0));
            })
            .ToList();
    }
}

public sealed class CreateCampaignCommandHandler(
    ICampaignRepository campaignRepository, ILessonPackageRepository packageRepository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<CreateCampaignCommand, CampaignDto>
{
    public async Task<CampaignDto> Handle(CreateCampaignCommand request, CancellationToken cancellationToken)
    {
        var package = await packageRepository.GetByIdAsync(request.LessonPackageId, cancellationToken)
            ?? throw new NotFoundException("LessonPackage", request.LessonPackageId.ToString());

        var existing = await campaignRepository.GetAllAsync(cancellationToken);
        var overlaps = existing.Any(c =>
            c.LessonPackageId == request.LessonPackageId &&
            request.StartsAtUtc < c.EndsAtUtc && request.EndsAtUtc > c.StartsAtUtc);
        if (overlaps)
            throw new DomainException(
                "campaign_overlap", "Bu paket için seçilen tarih aralığında zaten bir kampanya var.");

        var campaign = Campaign.Create(
            request.LessonPackageId, request.StartsAtUtc, request.EndsAtUtc, request.Price,
            request.MinLevel, request.MaxLevel);
        campaignRepository.Add(campaign);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CampaignDto(
            campaign.Id, campaign.LessonPackageId, package.Name, campaign.StartsAtUtc, campaign.EndsAtUtc,
            campaign.Price, campaign.MinLevel, campaign.MaxLevel, CampaignStatus.For(campaign, DateTimeOffset.UtcNow),
            0, 0);
    }
}

public sealed class UpdateCampaignCommandHandler(
    ICampaignRepository campaignRepository,
    ILessonPackageRepository packageRepository,
    ICustomerPackageRepository customerPackageRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCampaignCommand, CampaignDto>
{
    public async Task<CampaignDto> Handle(UpdateCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = await campaignRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Campaign", request.Id.ToString());

        var existing = await campaignRepository.GetAllAsync(cancellationToken);
        var overlaps = existing.Any(c =>
            c.Id != request.Id && c.LessonPackageId == campaign.LessonPackageId &&
            request.StartsAtUtc < c.EndsAtUtc && request.EndsAtUtc > c.StartsAtUtc);
        if (overlaps)
            throw new DomainException(
                "campaign_overlap", "Bu paket için seçilen tarih aralığında zaten bir kampanya var.");

        campaign.Update(request.StartsAtUtc, request.EndsAtUtc, request.Price, request.MinLevel, request.MaxLevel);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var package = await packageRepository.GetByIdAsync(campaign.LessonPackageId, cancellationToken);
        var participants = (await customerPackageRepository.GetAllAsync(cancellationToken))
            .Where(cp => cp.CampaignId == campaign.Id)
            .ToList();

        return new CampaignDto(
            campaign.Id, campaign.LessonPackageId, package?.Name ?? "—", campaign.StartsAtUtc, campaign.EndsAtUtc,
            campaign.Price, campaign.MinLevel, campaign.MaxLevel, CampaignStatus.For(campaign, DateTimeOffset.UtcNow),
            participants.Count, participants.Sum(p => p.PricePaid ?? 0));
    }
}

public sealed class DeleteCampaignCommandHandler(
    ICampaignRepository campaignRepository,
    ICustomerPackageRepository customerPackageRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCampaignCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = await campaignRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Campaign", request.Id.ToString());

        var hasParticipants = (await customerPackageRepository.GetAllAsync(cancellationToken))
            .Any(cp => cp.CampaignId == campaign.Id);
        if (hasParticipants)
            throw new DomainException(
                "campaign_has_purchases", "Bu kampanyadan satın alma yapılmış; silinemez.");

        campaignRepository.Remove(campaign);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
