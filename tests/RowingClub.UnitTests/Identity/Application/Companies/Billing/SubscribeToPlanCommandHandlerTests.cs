using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Billing;
using RowingClub.Identity.Application.Companies.Billing.SubscribeToPlan;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;
using RowingClub.UnitTests.Identity.Application;

namespace RowingClub.UnitTests.Identity.Application.Companies.Billing;

public sealed class SubscribeToPlanCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly ICompanySubscriptionRepository _subscriptionRepository = Substitute.For<ICompanySubscriptionRepository>();
    private readonly IIyzicoSubscriptionClient _iyzicoClient = Substitute.For<IIyzicoSubscriptionClient>();

    private SubscribeToPlanCommandHandler CreateHandler() =>
        new(_companyRepository, _subscriptionRepository, _iyzicoClient);

    [Fact]
    public async Task Throws_company_not_found_when_company_does_not_exist()
    {
        var companyId = Guid.NewGuid();
        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns((Company?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new SubscribeToPlanCommand(companyId, "Tayfa", "https://app.example.com/callback"), CancellationToken.None);

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
            new SubscribeToPlanCommand(company.Id, "NotAPlan", "https://app.example.com/callback"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_plan");
    }

    [Fact]
    public async Task Throws_invalid_plan_when_iyzico_does_not_support_the_plan()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _iyzicoClient.SupportsPlan(CompanyPlan.Mico).Returns(false);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new SubscribeToPlanCommand(company.Id, "Mico", "https://app.example.com/callback"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_plan");
    }

    [Fact]
    public async Task Throws_already_subscribed_when_company_has_active_subscription()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _iyzicoClient.SupportsPlan(CompanyPlan.Tayfa).Returns(true);

        var subscription = CompanySubscription.CreateEmpty(company.Id);
        subscription.Activate("cust-ref", "sub-ref", DateTimeOffset.UtcNow.AddMonths(1));
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(subscription);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new SubscribeToPlanCommand(company.Id, "Tayfa", "https://app.example.com/callback"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("already_subscribed");

        await _iyzicoClient.DidNotReceive().InitializeCheckoutFormAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CompanyPlan>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_checkout_init_failed_when_iyzico_returns_no_form_content()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _iyzicoClient.SupportsPlan(CompanyPlan.Tayfa).Returns(true);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns((CompanySubscription?)null);
        _iyzicoClient.InitializeCheckoutFormAsync(
                company.Id, company.Name, Arg.Any<string>(), CompanyPlan.Tayfa, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IyzicoCheckoutFormResult(string.Empty, "token-123"));

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new SubscribeToPlanCommand(company.Id, "Tayfa", "https://app.example.com/callback"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("checkout_init_failed");
    }

    [Fact]
    public async Task Returns_checkout_form_content_and_token_when_initialization_succeeds()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _iyzicoClient.SupportsPlan(CompanyPlan.Tayfa).Returns(true);
        _subscriptionRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns((CompanySubscription?)null);
        _iyzicoClient.InitializeCheckoutFormAsync(
                company.Id, company.Name, Arg.Any<string>(), CompanyPlan.Tayfa, "https://app.example.com/callback", Arg.Any<CancellationToken>())
            .Returns(new IyzicoCheckoutFormResult("<form>...</form>", "token-123"));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new SubscribeToPlanCommand(company.Id, "Tayfa", "https://app.example.com/callback"), CancellationToken.None);

        result.CheckoutFormContent.Should().Be("<form>...</form>");
        result.Token.Should().Be("token-123");
    }
}
