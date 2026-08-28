namespace RowingClub.Identity.Domain.Companies.Billing;

public interface ICompanyPaymentRepository
{
    Task<bool> ExistsByIyzicoPaymentReferenceCodeAsync(string referenceCode, CancellationToken cancellationToken);

    /// <summary>En yeni önce; firma panelindeki "Ödeme Geçmişi" listesi için.</summary>
    Task<List<CompanyPayment>> GetByCompanyIdAsync(Guid companyId, int take, CancellationToken cancellationToken);

    void Add(CompanyPayment payment);
}
