using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Companies.Billing.CancelPendingDowngrade;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.UnitTests.Identity.Application.Companies.Billing;

public sealed class CancelPendingDowngradeCommandHandlerTests
{
    private readonly ICompanySubscriptionRepository _subscriptionRepository = Substitute.For<ICompanySubscriptionRepository>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();

    private CancelPendingDowngradeCommandHandler CreateHandler() =>
        new(_subscriptionRepository, _auditLogger);

    [Fact]
    public async Task Throws_no_active_subscription_when_subscription_does_not_exist()
    {
        var companyId = Guid.NewGuid();
        _subscriptionRepository.GetByCompanyIdAsync(companyId, Arg.Any<CancellationToken>()).Returns((CompanySubscription?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(new CancelPendingDowngradeCommand(companyId), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("no_active_subscription");
    }

    [Fact]
    public async Task Clears_pending_plan_and_persists_when_subscription_exists()
    {
        var companyId = Guid.NewGuid();
        var subscription = CompanySubscription.CreateEmpty(companyId);
        subscription.Activate("customer-ref", "subscription-ref", DateTimeOffset.UtcNow.AddMonths(1));
        subscription.RequestPendingPlan(CompanyPlan.Mico, DateTimeOffset.UtcNow.AddMonths(1));
        _subscriptionRepository.GetByCompanyIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(subscription);

        var handler = CreateHandler();
        await handler.Handle(new CancelPendingDowngradeCommand(companyId), CancellationToken.None);

        subscription.PendingPlan.Should().BeNull();
        _subscriptionRepository.Received(1).Update(subscription);
        _auditLogger.Received(1).Log("COMPANY_PLAN_DOWNGRADE_CANCELLED", companyId.ToString(), Arg.Any<string>());
    }
}
