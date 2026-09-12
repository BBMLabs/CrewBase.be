using FluentAssertions;
using NSubstitute;
using RowingClub.Identity.Application.Platform;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;
using RowingClub.UnitTests.Identity.Application;

namespace RowingClub.UnitTests.Identity.Application.Platform;

public sealed class GetPlatformPaymentsQueryHandlerTests
{
    private readonly ICompanyPaymentRepository _paymentRepository = Substitute.For<ICompanyPaymentRepository>();
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();

    private GetPlatformPaymentsQueryHandler CreateHandler() => new(_paymentRepository, _companyRepository);

    [Fact]
    public async Task Returns_payments_with_company_names_and_failure_reasons()
    {
        var company = CompanyTestFactory.Create("Firma A");
        var succeeded = CompanyPayment.Succeeded(company.Id, CompanyPlan.Tayfa, 490, "TRY", CompanyPaymentKind.InitialSubscription, "ref-1");
        var failed = CompanyPayment.Failed(
            company.Id, CompanyPlan.Kaptan, 890, "TRY", CompanyPaymentKind.Upgrade, "ref-2", "Kart limiti yetersiz");

        _paymentRepository
            .GetPageAsync(Arg.Any<CompanyPaymentStatus?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([failed, succeeded]);
        _companyRepository
            .GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([company]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetPlatformPaymentsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].CompanyName.Should().Be("Firma A");
        result.Items[0].Status.Should().Be("Failed");
        result.Items[0].FailureReason.Should().Be("Kart limiti yetersiz");
        result.Items[1].Status.Should().Be("Succeeded");
        result.Items[1].FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task Passes_parsed_status_filter_to_repository()
    {
        _paymentRepository
            .GetPageAsync(Arg.Any<CompanyPaymentStatus?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _companyRepository
            .GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateHandler();
        await handler.Handle(new GetPlatformPaymentsQuery(Status: "failed"), CancellationToken.None);

        await _paymentRepository.Received(1).GetPageAsync(
            CompanyPaymentStatus.Failed, Arg.Any<DateTimeOffset?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ignores_invalid_status_filter()
    {
        _paymentRepository
            .GetPageAsync(Arg.Any<CompanyPaymentStatus?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _companyRepository
            .GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateHandler();
        await handler.Handle(new GetPlatformPaymentsQuery(Status: "not-a-status"), CancellationToken.None);

        await _paymentRepository.Received(1).GetPageAsync(
            null, Arg.Any<DateTimeOffset?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}

public sealed class GetPlatformPaymentStatsQueryHandlerTests
{
    private readonly ICompanyPaymentRepository _paymentRepository = Substitute.For<ICompanyPaymentRepository>();

    [Fact]
    public async Task Returns_counts_from_repository()
    {
        _paymentRepository.GetStatusCountsAsync(Arg.Any<CancellationToken>()).Returns(
            new Dictionary<CompanyPaymentStatus, int> { [CompanyPaymentStatus.Succeeded] = 7, [CompanyPaymentStatus.Failed] = 3 });

        var handler = new GetPlatformPaymentStatsQueryHandler(_paymentRepository);
        var result = await handler.Handle(new GetPlatformPaymentStatsQuery(), CancellationToken.None);

        result.Succeeded.Should().Be(7);
        result.Failed.Should().Be(3);
        result.Total.Should().Be(10);
    }

    [Fact]
    public async Task Returns_zero_when_no_payments_exist()
    {
        _paymentRepository.GetStatusCountsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetPlatformPaymentStatsQueryHandler(_paymentRepository);
        var result = await handler.Handle(new GetPlatformPaymentStatsQuery(), CancellationToken.None);

        result.Total.Should().Be(0);
        result.Succeeded.Should().Be(0);
        result.Failed.Should().Be(0);
    }
}
