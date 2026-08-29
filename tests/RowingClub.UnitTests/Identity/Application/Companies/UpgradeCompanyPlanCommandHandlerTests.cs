using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Billing;
using RowingClub.Identity.Application.Companies.UpgradeCompanyPlan;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;
using RowingClub.UnitTests.Identity.Application;

namespace RowingClub.UnitTests.Identity.Application.Companies;

public sealed class UpgradeCompanyPlanCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly ICompanySubscriptionRepository _subscriptionRepository = Substitute.For<ICompanySubscriptionRepository>();
    private readonly ICompanyPaymentRepository _paymentRepository = Substitute.For<ICompanyPaymentRepository>();
    private readonly IIyzicoSubscriptionClient _iyzicoClient = Substitute.For<IIyzicoSubscriptionClient>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();

    private UpgradeCompanyPlanCommandHandler CreateHandler() =>
        new(_companyRepository, _subscriptionRepository, _paymentRepository, _iyzicoClient, _auditLogger);

    private static CompanySubscription CreateActiveSubscription(Guid companyId, DateTimeOffset periodEnd)
    {
        var subscription = CompanySubscription.CreateEmpty(companyId);
        subscription.Activate("customer-ref", "subscription-ref", periodEnd);
        return subscription;
    }

    private static UpgradeCompanyPlanCommand CommandFor(Guid companyId, string plan) =>
        new(companyId, plan, UsedBranches: 1, UsedMembers: 10, UsedBoats: 2, UsedInstructors: 2,
            IdempotencyKey: "idempotency-key-1");

    [Fact]
    public async Task Throws_company_not_found_when_company_does_not_exist()
    {
        var companyId = Guid.NewGuid();
        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns((Company?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(CommandFor(companyId, "Kaptan"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("company_not_found");
    }

    [Fact]
    public async Task Throws_invalid_plan_when_plan_name_cannot_be_parsed()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var handler = CreateHandler();
        var act = () => handler.Handle(CommandFor(company.Id, "NotAPlan"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_plan");
    }

    [Fact]
    public async Task Throws_not_an_upgrade_when_target_plan_is_not_higher_than_current()
    {
        var company = CompanyTestFactory.Create();
        company.SetPlan(CompanyPlan.Kaptan, null, null, null, null);
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var handler = CreateHandler();
        var act = () => handler.Handle(CommandFor(company.Id, "Tayfa"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("not_an_upgrade");
    }

    [Fact]
    public async Task Throws_no_active_subscription_when_company_has_no_subscription()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns((CompanySubscription?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(CommandFor(company.Id, "Tayfa"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("no_active_subscription");
    }

    [Fact]
    public async Task Throws_no_active_subscription_when_subscription_exists_but_was_never_activated()
    {
        var company = CompanyTestFactory.Create();
        var subscription = CompanySubscription.CreateEmpty(company.Id);
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(subscription);

        var handler = CreateHandler();
        var act = () => handler.Handle(CommandFor(company.Id, "Tayfa"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("no_active_subscription");
    }

    [Fact]
    public async Task Throws_pending_downgrade_exists_when_a_downgrade_is_already_pending()
    {
        var company = CompanyTestFactory.Create();
        var periodEnd = DateTimeOffset.UtcNow.AddDays(20);
        var subscription = CreateActiveSubscription(company.Id, periodEnd);
        subscription.RequestPendingPlan(CompanyPlan.Mico, periodEnd);

        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(subscription);

        var handler = CreateHandler();
        var act = () => handler.Handle(CommandFor(company.Id, "Kaptan"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("pending_downgrade_exists");

        await _iyzicoClient.DidNotReceive().UpgradeSubscriptionAsync(
            Arg.Any<string>(), Arg.Any<CompanyPlan>(), Arg.Any<IyzicoUpgradePeriod>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_plan_limits_exceeded_and_never_charges_when_usage_does_not_fit_target_plan()
    {
        var company = CompanyTestFactory.Create();
        var periodEnd = DateTimeOffset.UtcNow.AddDays(20);
        var subscription = CreateActiveSubscription(company.Id, periodEnd);

        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(subscription);

        var command = new UpgradeCompanyPlanCommand(
            company.Id, "Tayfa", UsedBranches: 99, UsedMembers: 10, UsedBoats: 2, UsedInstructors: 2,
            IdempotencyKey: "idempotency-key-1");

        var handler = CreateHandler();
        var act = () => handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("plan_limits_exceeded");

        await _iyzicoClient.DidNotReceive().UpgradeSubscriptionAsync(
            Arg.Any<string>(), Arg.Any<CompanyPlan>(), Arg.Any<IyzicoUpgradePeriod>(), Arg.Any<CancellationToken>());
        _companyRepository.DidNotReceive().Update(Arg.Any<Company>());
    }

    [Fact]
    public async Task Throws_payment_failed_and_never_changes_plan_when_charge_is_rejected()
    {
        var company = CompanyTestFactory.Create();
        var periodEnd = DateTimeOffset.UtcNow.AddDays(20);
        var subscription = CreateActiveSubscription(company.Id, periodEnd);

        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        _iyzicoClient.UpgradeSubscriptionAsync(
                "subscription-ref", CompanyPlan.Tayfa, IyzicoUpgradePeriod.Now, Arg.Any<CancellationToken>())
            .Returns(new IyzicoSubscriptionResult(
                Success: false, CustomerReferenceCode: null, SubscriptionReferenceCode: null,
                CurrentPeriodEndUtc: null, ChargedAmount: null, PaymentReferenceCode: null,
                ErrorMessage: "Yetersiz bakiye"));

        var handler = CreateHandler();
        var act = () => handler.Handle(CommandFor(company.Id, "Tayfa"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("payment_failed");
        ex.Which.Message.Should().Be("Yetersiz bakiye");

        company.Plan.Should().Be(CompanyPlan.Mico);
        _companyRepository.DidNotReceive().Update(Arg.Any<Company>());
        _paymentRepository.DidNotReceive().Add(Arg.Any<CompanyPayment>());
    }

    [Fact]
    public async Task Upgrades_plan_charges_and_records_renewal_when_payment_succeeds()
    {
        var company = CompanyTestFactory.Create();
        var currentPeriodEnd = DateTimeOffset.UtcNow.AddDays(20);
        var newPeriodEnd = DateTimeOffset.UtcNow.AddMonths(1);
        var subscription = CreateActiveSubscription(company.Id, currentPeriodEnd);

        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        _iyzicoClient.UpgradeSubscriptionAsync(
                "subscription-ref", CompanyPlan.Tayfa, IyzicoUpgradePeriod.Now, Arg.Any<CancellationToken>())
            .Returns(new IyzicoSubscriptionResult(
                Success: true, CustomerReferenceCode: "customer-ref", SubscriptionReferenceCode: "subscription-ref",
                CurrentPeriodEndUtc: newPeriodEnd, ChargedAmount: 400m, PaymentReferenceCode: "payment-ref-2",
                ErrorMessage: null));
        _paymentRepository.ExistsByIyzicoPaymentReferenceCodeAsync("payment-ref-2", Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = CreateHandler();
        var result = await handler.Handle(CommandFor(company.Id, "Tayfa"), CancellationToken.None);

        company.Plan.Should().Be(CompanyPlan.Tayfa);
        result.Plan.Should().Be(CompanyPlan.Tayfa.ToString());
        result.ChargedAmount.Should().Be(400m);
        result.MaxBranches.Should().Be(1);
        result.MaxMembers.Should().Be(50);
        result.MaxBoats.Should().Be(8);
        result.NextPaymentDateUtc.Should().Be(newPeriodEnd);

        _companyRepository.Received(1).Update(company);
        subscription.CurrentPeriodEndUtc.Should().Be(newPeriodEnd);
        _subscriptionRepository.Received(1).Update(subscription);
        _paymentRepository.Received(1).Add(Arg.Is<CompanyPayment>(p =>
            p!.IyzicoPaymentReferenceCode == "payment-ref-2" && p.Kind == CompanyPaymentKind.Upgrade && p.Amount == 400m));
        _auditLogger.Received(1).Log("COMPANY_PLAN_CHANGED", company.Id.ToString(), Arg.Any<string>());
    }

    [Fact]
    public async Task Does_not_record_renewal_when_charge_result_has_no_new_period_end()
    {
        var company = CompanyTestFactory.Create();
        var currentPeriodEnd = DateTimeOffset.UtcNow.AddDays(20);
        var subscription = CreateActiveSubscription(company.Id, currentPeriodEnd);

        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        _iyzicoClient.UpgradeSubscriptionAsync(
                "subscription-ref", CompanyPlan.Tayfa, IyzicoUpgradePeriod.Now, Arg.Any<CancellationToken>())
            .Returns(new IyzicoSubscriptionResult(
                Success: true, CustomerReferenceCode: "customer-ref", SubscriptionReferenceCode: "subscription-ref",
                CurrentPeriodEndUtc: null, ChargedAmount: 400m, PaymentReferenceCode: "payment-ref-3",
                ErrorMessage: null));
        _paymentRepository.ExistsByIyzicoPaymentReferenceCodeAsync("payment-ref-3", Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = CreateHandler();
        var result = await handler.Handle(CommandFor(company.Id, "Tayfa"), CancellationToken.None);

        subscription.CurrentPeriodEndUtc.Should().Be(currentPeriodEnd);
        result.NextPaymentDateUtc.Should().Be(currentPeriodEnd);
        _subscriptionRepository.DidNotReceive().Update(Arg.Any<CompanySubscription>());
    }

    [Fact]
    public async Task Does_not_duplicate_payment_when_payment_reference_already_recorded()
    {
        var company = CompanyTestFactory.Create();
        var currentPeriodEnd = DateTimeOffset.UtcNow.AddDays(20);
        var newPeriodEnd = DateTimeOffset.UtcNow.AddMonths(1);
        var subscription = CreateActiveSubscription(company.Id, currentPeriodEnd);

        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        _iyzicoClient.UpgradeSubscriptionAsync(
                "subscription-ref", CompanyPlan.Tayfa, IyzicoUpgradePeriod.Now, Arg.Any<CancellationToken>())
            .Returns(new IyzicoSubscriptionResult(
                Success: true, CustomerReferenceCode: "customer-ref", SubscriptionReferenceCode: "subscription-ref",
                CurrentPeriodEndUtc: newPeriodEnd, ChargedAmount: 400m, PaymentReferenceCode: "payment-ref-4",
                ErrorMessage: null));
        _paymentRepository.ExistsByIyzicoPaymentReferenceCodeAsync("payment-ref-4", Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = CreateHandler();
        await handler.Handle(CommandFor(company.Id, "Tayfa"), CancellationToken.None);

        _paymentRepository.DidNotReceive().Add(Arg.Any<CompanyPayment>());
        company.Plan.Should().Be(CompanyPlan.Tayfa);
    }
}
