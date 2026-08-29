using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Billing;
using RowingClub.Identity.Application.Companies.Billing.ConfirmSubscriptionCheckout;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;
using RowingClub.UnitTests.Identity.Application;

namespace RowingClub.UnitTests.Identity.Application.Companies.Billing;

public sealed class ConfirmSubscriptionCheckoutCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly ICompanySubscriptionRepository _subscriptionRepository = Substitute.For<ICompanySubscriptionRepository>();
    private readonly ICompanyPaymentRepository _paymentRepository = Substitute.For<ICompanyPaymentRepository>();
    private readonly IIyzicoSubscriptionClient _iyzicoClient = Substitute.For<IIyzicoSubscriptionClient>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();

    private ConfirmSubscriptionCheckoutCommandHandler CreateHandler() =>
        new(_companyRepository, _subscriptionRepository, _paymentRepository, _iyzicoClient, _auditLogger);

    private static IyzicoSubscriptionResult SuccessfulResult(
        DateTimeOffset periodEnd, string paymentReferenceCode = "payment-ref-1") =>
        new(
            Success: true,
            CustomerReferenceCode: "customer-ref",
            SubscriptionReferenceCode: "subscription-ref",
            CurrentPeriodEndUtc: periodEnd,
            ChargedAmount: 490m,
            PaymentReferenceCode: paymentReferenceCode,
            ErrorMessage: null);

    [Fact]
    public async Task Throws_company_not_found_when_company_does_not_exist()
    {
        var companyId = Guid.NewGuid();
        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns((Company?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new ConfirmSubscriptionCheckoutCommand(companyId, "Tayfa", "token-123"), CancellationToken.None);

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
            new ConfirmSubscriptionCheckoutCommand(company.Id, "NotAPlan", "token-123"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_plan");
    }

    [Fact]
    public async Task Throws_checkout_failed_when_iyzico_result_is_unsuccessful()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _iyzicoClient.RetrieveCheckoutFormResultAsync("token-123", Arg.Any<CancellationToken>())
            .Returns(new IyzicoSubscriptionResult(
                Success: false, CustomerReferenceCode: null, SubscriptionReferenceCode: null,
                CurrentPeriodEndUtc: null, ChargedAmount: null, PaymentReferenceCode: null,
                ErrorMessage: "Kart reddedildi"));

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new ConfirmSubscriptionCheckoutCommand(company.Id, "Tayfa", "token-123"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("checkout_failed");
        ex.Which.Message.Should().Be("Kart reddedildi");

        _subscriptionRepository.DidNotReceive().Add(Arg.Any<CompanySubscription>());
        _paymentRepository.DidNotReceive().Add(Arg.Any<CompanyPayment>());
    }

    [Fact]
    public async Task Activates_subscription_sets_plan_and_records_payment_on_first_confirmation()
    {
        var company = CompanyTestFactory.Create();
        var periodEnd = DateTimeOffset.UtcNow.AddMonths(1);
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns((CompanySubscription?)null);
        _iyzicoClient.RetrieveCheckoutFormResultAsync("token-123", Arg.Any<CancellationToken>())
            .Returns(SuccessfulResult(periodEnd));
        _paymentRepository.ExistsByIyzicoPaymentReferenceCodeAsync("payment-ref-1", Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ConfirmSubscriptionCheckoutCommand(company.Id, "Tayfa", "token-123"), CancellationToken.None);

        result.Plan.Should().Be(CompanyPlan.Tayfa.ToString());
        result.NextPaymentDateUtc.Should().Be(periodEnd);
        company.Plan.Should().Be(CompanyPlan.Tayfa);
        _subscriptionRepository.Received(1).Add(Arg.Is<CompanySubscription>(s =>
            s!.Status == CompanySubscriptionStatus.Active && s.CurrentPeriodEndUtc == periodEnd));
        _paymentRepository.Received(1).Add(Arg.Is<CompanyPayment>(p =>
            p!.IyzicoPaymentReferenceCode == "payment-ref-1" && p.Kind == CompanyPaymentKind.InitialSubscription));
        _companyRepository.Received(1).Update(company);
        _auditLogger.Received(1).Log("COMPANY_SUBSCRIPTION_ACTIVATED", company.Id.ToString(), Arg.Any<string>());
    }

    [Fact]
    public async Task Does_not_duplicate_payment_when_confirming_the_same_checkout_twice()
    {
        var company = CompanyTestFactory.Create();
        var periodEnd = DateTimeOffset.UtcNow.AddMonths(1);
        var existingSubscription = CompanySubscription.CreateEmpty(company.Id);
        existingSubscription.Activate("customer-ref", "subscription-ref", periodEnd);

        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(existingSubscription);
        _iyzicoClient.RetrieveCheckoutFormResultAsync("token-123", Arg.Any<CancellationToken>())
            .Returns(SuccessfulResult(periodEnd));
        _paymentRepository.ExistsByIyzicoPaymentReferenceCodeAsync("payment-ref-1", Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ConfirmSubscriptionCheckoutCommand(company.Id, "Tayfa", "token-123"), CancellationToken.None);

        result.Plan.Should().Be(CompanyPlan.Tayfa.ToString());
        result.NextPaymentDateUtc.Should().Be(periodEnd);
        _subscriptionRepository.DidNotReceive().Add(Arg.Any<CompanySubscription>());
        _paymentRepository.DidNotReceive().Add(Arg.Any<CompanyPayment>());
        _subscriptionRepository.Received(1).Update(existingSubscription);
    }
}
