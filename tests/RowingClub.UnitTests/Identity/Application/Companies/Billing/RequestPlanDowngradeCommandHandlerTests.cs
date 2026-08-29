using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Companies.Billing.RequestPlanDowngrade;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;
using RowingClub.UnitTests.Identity.Application;

namespace RowingClub.UnitTests.Identity.Application.Companies.Billing;

public sealed class RequestPlanDowngradeCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly ICompanySubscriptionRepository _subscriptionRepository = Substitute.For<ICompanySubscriptionRepository>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();

    private RequestPlanDowngradeCommandHandler CreateHandler() =>
        new(_companyRepository, _subscriptionRepository, _auditLogger);

    private static CompanySubscription CreateActiveSubscription(Guid companyId, DateTimeOffset periodEnd)
    {
        var subscription = CompanySubscription.CreateEmpty(companyId);
        subscription.Activate("customer-ref", "subscription-ref", periodEnd);
        return subscription;
    }

    [Fact]
    public async Task Throws_company_not_found_when_company_does_not_exist()
    {
        var companyId = Guid.NewGuid();
        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns((Company?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new RequestPlanDowngradeCommand(companyId, "Mico", 0, 0, 0), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("company_not_found");
    }

    [Fact]
    public async Task Throws_invalid_plan_when_plan_name_cannot_be_parsed()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new RequestPlanDowngradeCommand(company.Id, "NotAPlan", 0, 0, 0), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_plan");
    }

    [Fact]
    public async Task Throws_invalid_plan_when_target_plan_is_not_self_serve()
    {
        var company = CompanyTestFactory.Create();
        company.SetPlan(CompanyPlan.Kaptan, null, null, null);
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new RequestPlanDowngradeCommand(company.Id, "Custom", 0, 0, 0), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_plan");
    }

    [Fact]
    public async Task Throws_not_a_downgrade_when_target_plan_is_not_lower_than_current()
    {
        var company = CompanyTestFactory.Create();
        company.SetPlan(CompanyPlan.Tayfa, null, null, null);
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new RequestPlanDowngradeCommand(company.Id, "Kaptan", 0, 0, 0), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("not_a_downgrade");
    }

    [Fact]
    public async Task Throws_no_active_subscription_when_company_has_no_subscription()
    {
        var company = CompanyTestFactory.Create();
        company.SetPlan(CompanyPlan.Kaptan, null, null, null);
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns((CompanySubscription?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new RequestPlanDowngradeCommand(company.Id, "Tayfa", 0, 0, 0), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("no_active_subscription");
    }

    [Fact]
    public async Task Requests_pending_plan_and_returns_effective_date_when_valid_downgrade()
    {
        var company = CompanyTestFactory.Create();
        company.SetPlan(CompanyPlan.Kaptan, null, null, null);
        var periodEnd = DateTimeOffset.UtcNow.AddDays(15);
        var subscription = CreateActiveSubscription(company.Id, periodEnd);

        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(subscription);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RequestPlanDowngradeCommand(company.Id, "Tayfa", UsedBranches: 1, UsedMembers: 10, UsedBoats: 2),
            CancellationToken.None);

        result.PendingPlan.Should().Be(CompanyPlan.Tayfa.ToString());
        result.EffectiveAtUtc.Should().Be(periodEnd);
        subscription.PendingPlan.Should().Be(CompanyPlan.Tayfa);
        _subscriptionRepository.Received(1).Update(subscription);
        _auditLogger.Received(1).Log("COMPANY_PLAN_DOWNGRADE_REQUESTED", company.Id.ToString(), Arg.Any<string>());
    }
}
