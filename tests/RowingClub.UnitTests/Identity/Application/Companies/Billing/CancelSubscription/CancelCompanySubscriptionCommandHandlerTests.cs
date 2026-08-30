using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Billing;
using RowingClub.Identity.Application.Companies.Billing.CancelSubscription;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;
using RowingClub.UnitTests.Identity.Application;

namespace RowingClub.UnitTests.Identity.Application.Companies.Billing.CancelSubscription;

public sealed class CancelCompanySubscriptionCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly ICompanySubscriptionRepository _subscriptionRepository = Substitute.For<ICompanySubscriptionRepository>();
    private readonly IIyzicoSubscriptionClient _iyzicoClient = Substitute.For<IIyzicoSubscriptionClient>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();

    private CancelCompanySubscriptionCommandHandler CreateHandler() =>
        new(_companyRepository, _subscriptionRepository, _iyzicoClient, _auditLogger);

    [Fact]
    public async Task Throws_company_not_found_when_company_does_not_exist()
    {
        var companyId = Guid.NewGuid();
        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns((Company?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(new CancelCompanySubscriptionCommand(companyId), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("company_not_found");
    }

    [Fact]
    public async Task Throws_no_active_subscription_when_subscription_is_none()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns((CompanySubscription?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(new CancelCompanySubscriptionCommand(company.Id), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("no_active_subscription");
    }

    [Fact]
    public async Task Throws_iyzico_cancel_failed_when_iyzico_call_fails()
    {
        var company = CompanyTestFactory.Create();
        company.SetPlan(CompanyPlan.Kaptan, null, null, null, null);
        var subscription = CompanySubscription.CreateEmpty(company.Id);
        subscription.Activate("customer-ref", "subscription-ref", DateTimeOffset.UtcNow.AddDays(20));

        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        _iyzicoClient.CancelSubscriptionAsync("subscription-ref", Arg.Any<CancellationToken>()).Returns(false);

        var handler = CreateHandler();
        var act = () => handler.Handle(new CancelCompanySubscriptionCommand(company.Id), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("iyzico_cancel_failed");
        subscription.Status.Should().Be(CompanySubscriptionStatus.Active);
    }

    [Fact]
    public async Task Cancels_subscription_and_downgrades_to_mico_when_valid()
    {
        var company = CompanyTestFactory.Create();
        company.SetPlan(CompanyPlan.Kaptan, null, null, null, null);
        var subscription = CompanySubscription.CreateEmpty(company.Id);
        subscription.Activate("customer-ref", "subscription-ref", DateTimeOffset.UtcNow.AddDays(20));

        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        _iyzicoClient.CancelSubscriptionAsync("subscription-ref", Arg.Any<CancellationToken>()).Returns(true);

        var handler = CreateHandler();
        await handler.Handle(new CancelCompanySubscriptionCommand(company.Id), CancellationToken.None);

        subscription.Status.Should().Be(CompanySubscriptionStatus.Cancelled);
        company.Plan.Should().Be(CompanyPlan.Mico);
        _subscriptionRepository.Received(1).Update(subscription);
        _companyRepository.Received(1).Update(company);
        _auditLogger.Received(1).Log("COMPANY_SUBSCRIPTION_CANCELLED", company.Id.ToString(), Arg.Any<string>());
    }
}
