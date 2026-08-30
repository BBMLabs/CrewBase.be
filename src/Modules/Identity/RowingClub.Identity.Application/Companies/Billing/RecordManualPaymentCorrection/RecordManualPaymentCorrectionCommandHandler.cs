using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;

namespace RowingClub.Identity.Application.Companies.Billing.RecordManualPaymentCorrection;

public sealed class RecordManualPaymentCorrectionCommandHandler(
    ICompanyRepository companyRepository, ICompanyPaymentRepository paymentRepository, IAuditLogger auditLogger)
    : IRequestHandler<RecordManualPaymentCorrectionCommand, Unit>
{
    public async Task<Unit> Handle(RecordManualPaymentCorrectionCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new DomainException("company_not_found", "Şirket bulunamadı.");

        if (request.Amount <= 0)
            throw new DomainException("invalid_amount", "Tutar sıfırdan büyük olmalıdır.");

        if (string.IsNullOrWhiteSpace(request.Note))
            throw new DomainException("note_required", "Manuel düzeltme için bir gerekçe girilmelidir.");

        if (!Enum.TryParse<CompanyPaymentKind>(request.Kind, ignoreCase: true, out var kind))
            throw new DomainException("invalid_kind", "Geçersiz ödeme türü.");

        if (!Enum.TryParse<CompanyPaymentStatus>(request.Status, ignoreCase: true, out var status))
            throw new DomainException("invalid_status", "Geçersiz ödeme durumu.");

        var payment = CompanyPayment.RecordManualCorrection(
            company.Id, company.Plan, request.Amount, request.Currency, kind, status, request.Note);
        paymentRepository.Add(payment);

        auditLogger.Log(
            "COMPANY_PAYMENT_MANUAL_CORRECTION", company.Id.ToString(),
            $"Master admin manuel ödeme kaydı ekledi: {request.Amount} {request.Currency} ({request.Status})");

        return Unit.Value;
    }
}
