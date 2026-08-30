using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Application.Companies.Billing.RecordManualPaymentCorrection;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;
using RowingClub.UnitTests.Identity.Application;

namespace RowingClub.UnitTests.Identity.Application.Companies.Billing.RecordManualPaymentCorrection;

public sealed class RecordManualPaymentCorrectionCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly ICompanyPaymentRepository _paymentRepository = Substitute.For<ICompanyPaymentRepository>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();

    private RecordManualPaymentCorrectionCommandHandler CreateHandler() =>
        new(_companyRepository, _paymentRepository, _auditLogger);

    [Fact]
    public async Task Throws_company_not_found_when_company_does_not_exist()
    {
        var companyId = Guid.NewGuid();
        _companyRepository.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns((Company?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new RecordManualPaymentCorrectionCommand(companyId, 100, "TRY", "SubscriptionRenewal", "Succeeded", "not"),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("company_not_found");
    }

    [Fact]
    public async Task Throws_invalid_amount_when_amount_is_not_positive()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new RecordManualPaymentCorrectionCommand(company.Id, 0, "TRY", "SubscriptionRenewal", "Succeeded", "not"),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_amount");
    }

    [Fact]
    public async Task Throws_note_required_when_note_is_blank()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new RecordManualPaymentCorrectionCommand(company.Id, 100, "TRY", "SubscriptionRenewal", "Succeeded", "  "),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("note_required");
    }

    [Fact]
    public async Task Throws_invalid_kind_when_kind_cannot_be_parsed()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new RecordManualPaymentCorrectionCommand(company.Id, 100, "TRY", "NotAKind", "Succeeded", "not"),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_kind");
    }

    [Fact]
    public async Task Throws_invalid_status_when_status_cannot_be_parsed()
    {
        var company = CompanyTestFactory.Create();
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new RecordManualPaymentCorrectionCommand(company.Id, 100, "TRY", "SubscriptionRenewal", "NotAStatus", "not"),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_status");
    }

    [Fact]
    public async Task Adds_manual_correction_payment_when_valid()
    {
        var company = CompanyTestFactory.Create();
        company.SetPlan(CompanyPlan.Kaptan, null, null, null, null);
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var handler = CreateHandler();
        await handler.Handle(
            new RecordManualPaymentCorrectionCommand(
                company.Id, 890, "TRY", "SubscriptionRenewal", "Succeeded", "iyzico webhook kaçırıldı"),
            CancellationToken.None);

        _paymentRepository.Received(1).Add(Arg.Is<CompanyPayment>(p =>
            p != null && p.CompanyId == company.Id && p.Amount == 890 && p.Currency == "TRY" &&
            p.Plan == CompanyPlan.Kaptan && p.Status == CompanyPaymentStatus.Succeeded &&
            p.IyzicoPaymentReferenceCode == null && p.FailureReason == "iyzico webhook kaçırıldı"));
        _auditLogger.Received(1).Log("COMPANY_PAYMENT_MANUAL_CORRECTION", company.Id.ToString(), Arg.Any<string>());
    }
}
