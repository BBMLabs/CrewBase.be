using FluentAssertions;
using NSubstitute;
using RowingClub.Identity.Application.Platform;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;
using RowingClub.UnitTests.Identity.Application;

namespace RowingClub.UnitTests.Identity.Application.Platform;

public sealed class GetPlatformRevenueQueryHandlerTests
{
    private readonly ICompanyPaymentRepository _paymentRepository = Substitute.For<ICompanyPaymentRepository>();
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();

    private GetPlatformRevenueQueryHandler CreateHandler() => new(_paymentRepository, _companyRepository);

    [Fact]
    public async Task Aggregates_revenue_per_company_and_sorts_by_total_descending()
    {
        var companyA = CompanyTestFactory.Create("Firma A");
        var companyB = CompanyTestFactory.Create("Firma B");
        var now = DateTimeOffset.UtcNow;

        var payments = new List<CompanyPayment>
        {
            CompanyPayment.Succeeded(companyA.Id, CompanyPlan.Tayfa, 490, "TRY", CompanyPaymentKind.InitialSubscription, "ref-1"),
            CompanyPayment.Succeeded(companyA.Id, CompanyPlan.Tayfa, 490, "TRY", CompanyPaymentKind.SubscriptionRenewal, "ref-2"),
            CompanyPayment.Succeeded(companyB.Id, CompanyPlan.Amiral, 1490, "TRY", CompanyPaymentKind.InitialSubscription, "ref-3"),
        };
        _paymentRepository.GetAllSucceededAsync(Arg.Any<CancellationToken>()).Returns(payments);
        _companyRepository
            .GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([companyA, companyB]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetPlatformRevenueQuery(), CancellationToken.None);

        result.TotalRevenueAllTime.Should().Be(490 + 490 + 1490);
        result.TotalSucceededPayments.Should().Be(3);
        result.Companies.Should().HaveCount(2);
        result.Companies[0].CompanyId.Should().Be(companyB.Id);
        result.Companies[0].TotalRevenue.Should().Be(1490);
        result.Companies[0].PaymentCount.Should().Be(1);
        result.Companies[1].CompanyId.Should().Be(companyA.Id);
        result.Companies[1].TotalRevenue.Should().Be(980);
        result.Companies[1].PaymentCount.Should().Be(2);
    }

    [Fact]
    public async Task Returns_zero_totals_when_no_payments_exist()
    {
        _paymentRepository.GetAllSucceededAsync(Arg.Any<CancellationToken>()).Returns([]);
        _companyRepository
            .GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetPlatformRevenueQuery(), CancellationToken.None);

        result.TotalRevenueAllTime.Should().Be(0);
        result.TotalRevenueThisMonth.Should().Be(0);
        result.TotalSucceededPayments.Should().Be(0);
        result.Companies.Should().BeEmpty();
    }
}
