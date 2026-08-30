using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Application.Panel;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Campaigns;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Packages;

namespace RowingClub.UnitTests.Scheduling.Application.Panel;

public sealed class CreateCampaignCommandHandlerTests
{
    private readonly ICampaignRepository _campaignRepository = Substitute.For<ICampaignRepository>();
    private readonly ILessonPackageRepository _packageRepository = Substitute.For<ILessonPackageRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();

    private CreateCampaignCommandHandler CreateHandler() => new(_campaignRepository, _packageRepository, _unitOfWork);

    private static LessonPackage CreatePackage() => LessonPackage.Create("Paket", null, 8, 100m, null);

    [Fact]
    public async Task Throws_not_found_when_package_does_not_exist()
    {
        var packageId = Guid.NewGuid();
        _packageRepository.GetByIdAsync(packageId, Arg.Any<CancellationToken>()).Returns((LessonPackage?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new CreateCampaignCommand(packageId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(5), 50m, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_campaign_overlap_when_dates_overlap_an_existing_campaign_for_same_package()
    {
        var package = CreatePackage();
        _packageRepository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);
        var existing = Campaign.Create(package.Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(10), 50m, null, null);
        _campaignRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([existing]);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new CreateCampaignCommand(
                package.Id, DateTimeOffset.UtcNow.AddDays(5), DateTimeOffset.UtcNow.AddDays(15), 60m, null, null),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("campaign_overlap");
    }

    [Fact]
    public async Task Creates_campaign_and_persists_when_valid()
    {
        var package = CreatePackage();
        _packageRepository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);
        _campaignRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var handler = CreateHandler();
        var starts = DateTimeOffset.UtcNow;
        var ends = DateTimeOffset.UtcNow.AddDays(10);
        var result = await handler.Handle(
            new CreateCampaignCommand(package.Id, starts, ends, 50m, 2, 8), CancellationToken.None);

        result.LessonPackageId.Should().Be(package.Id);
        result.PackageName.Should().Be(package.Name);
        result.Price.Should().Be(50m);
        result.MinLevel.Should().Be(2);
        result.MaxLevel.Should().Be(8);
        result.Status.Should().Be("Active");
        result.ParticipantCount.Should().Be(0);
        _campaignRepository.Received(1).Add(Arg.Is<Campaign>(c => c != null && c.LessonPackageId == package.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class UpdateCampaignCommandHandlerTests
{
    private readonly ICampaignRepository _campaignRepository = Substitute.For<ICampaignRepository>();
    private readonly ILessonPackageRepository _packageRepository = Substitute.For<ILessonPackageRepository>();
    private readonly ICustomerPackageRepository _customerPackageRepository = Substitute.For<ICustomerPackageRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();

    private UpdateCampaignCommandHandler CreateHandler() =>
        new(_campaignRepository, _packageRepository, _customerPackageRepository, _unitOfWork);

    [Fact]
    public async Task Throws_not_found_when_campaign_does_not_exist()
    {
        var campaignId = Guid.NewGuid();
        _campaignRepository.GetByIdAsync(campaignId, Arg.Any<CancellationToken>()).Returns((Campaign?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new UpdateCampaignCommand(campaignId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(5), 50m, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_campaign_overlap_when_dates_overlap_a_different_campaign_for_same_package()
    {
        var packageId = Guid.NewGuid();
        var campaign = Campaign.Create(packageId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(5), 50m, null, null);
        var other = Campaign.Create(
            packageId, DateTimeOffset.UtcNow.AddDays(10), DateTimeOffset.UtcNow.AddDays(20), 60m, null, null);
        _campaignRepository.GetByIdAsync(campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);
        _campaignRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([campaign, other]);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new UpdateCampaignCommand(
                campaign.Id, DateTimeOffset.UtcNow.AddDays(12), DateTimeOffset.UtcNow.AddDays(15), 70m, null, null),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("campaign_overlap");
    }

    [Fact]
    public async Task Does_not_overlap_against_itself_and_updates_when_valid()
    {
        var package = LessonPackage.Create("Paket", null, 8, 100m, null);
        var campaign = Campaign.Create(package.Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(5), 50m, null, null);
        _campaignRepository.GetByIdAsync(campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);
        _campaignRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([campaign]);
        _packageRepository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);
        _customerPackageRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var handler = CreateHandler();
        var newStarts = DateTimeOffset.UtcNow.AddDays(1);
        var newEnds = DateTimeOffset.UtcNow.AddDays(6);
        var result = await handler.Handle(
            new UpdateCampaignCommand(campaign.Id, newStarts, newEnds, 80m, 1, 5), CancellationToken.None);

        result.Price.Should().Be(80m);
        result.MinLevel.Should().Be(1);
        result.MaxLevel.Should().Be(5);
        result.PackageName.Should().Be(package.Name);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class GetCampaignsQueryHandlerTests
{
    private readonly ICampaignRepository _campaignRepository = Substitute.For<ICampaignRepository>();
    private readonly ILessonPackageRepository _packageRepository = Substitute.For<ILessonPackageRepository>();
    private readonly ICustomerPackageRepository _customerPackageRepository = Substitute.For<ICustomerPackageRepository>();

    private GetCampaignsQueryHandler CreateHandler() =>
        new(_campaignRepository, _packageRepository, _customerPackageRepository);

    [Fact]
    public async Task Computes_status_participant_count_and_revenue_per_campaign()
    {
        var package = LessonPackage.Create("Paket", null, 8, 100m, null);
        var activeCampaign = Campaign.Create(
            package.Id, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(5), 50m, null, null);
        var endedCampaign = Campaign.Create(
            package.Id, DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-5), 40m, null, null);

        _campaignRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([activeCampaign, endedCampaign]);
        _packageRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([package]);

        var customer = Customer.Create("Ada Lovelace", "+905551112233", "ada@example.com");
        var participant1 = CustomerPackage.Assign(
            customer.Id, package, CustomerPackageSource.Purchased, "ref-1", 50m, activeCampaign.Id);
        var participant2 = CustomerPackage.Assign(
            customer.Id, package, CustomerPackageSource.Purchased, "ref-2", 50m, activeCampaign.Id);
        _customerPackageRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([participant1, participant2]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetCampaignsQuery(), CancellationToken.None);

        var active = result.Single(c => c.Id == activeCampaign.Id);
        active.Status.Should().Be("Active");
        active.ParticipantCount.Should().Be(2);
        active.TotalRevenue.Should().Be(100m);

        var ended = result.Single(c => c.Id == endedCampaign.Id);
        ended.Status.Should().Be("Ended");
        ended.ParticipantCount.Should().Be(0);
        ended.TotalRevenue.Should().Be(0);
    }
}

public sealed class DeleteCampaignCommandHandlerTests
{
    private readonly ICampaignRepository _campaignRepository = Substitute.For<ICampaignRepository>();
    private readonly ICustomerPackageRepository _customerPackageRepository = Substitute.For<ICustomerPackageRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();

    private DeleteCampaignCommandHandler CreateHandler() =>
        new(_campaignRepository, _customerPackageRepository, _unitOfWork);

    [Fact]
    public async Task Throws_campaign_has_purchases_when_customers_already_participated()
    {
        var campaign = Campaign.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(5), 50m, null, null);
        _campaignRepository.GetByIdAsync(campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);

        var package = LessonPackage.Create("Paket", null, 8, 100m, null);
        var customer = Customer.Create("Ada Lovelace", "+905551112233", "ada@example.com");
        var purchased = CustomerPackage.Assign(
            customer.Id, package, CustomerPackageSource.Purchased, "ref-1", 50m, campaign.Id);
        _customerPackageRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([purchased]);

        var handler = CreateHandler();
        var act = () => handler.Handle(new DeleteCampaignCommand(campaign.Id), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("campaign_has_purchases");
        _campaignRepository.DidNotReceive().Remove(Arg.Any<Campaign>());
    }

    [Fact]
    public async Task Removes_campaign_when_no_participants()
    {
        var campaign = Campaign.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(5), 50m, null, null);
        _campaignRepository.GetByIdAsync(campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);
        _customerPackageRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var handler = CreateHandler();
        await handler.Handle(new DeleteCampaignCommand(campaign.Id), CancellationToken.None);

        _campaignRepository.Received(1).Remove(campaign);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
