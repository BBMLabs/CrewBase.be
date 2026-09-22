namespace RowingClub.Identity.Domain.Companies.Billing;

public interface ICompanyPaymentRepository
{
    Task<bool> ExistsByIyzicoPaymentReferenceCodeAsync(string referenceCode, CancellationToken cancellationToken);

    /// <summary>En yeni önce; firma panelindeki "Ödeme Geçmişi" listesi için.</summary>
    Task<List<CompanyPayment>> GetPageByCompanyIdAsync(
        Guid companyId, DateTimeOffset? cursorOccurredAtUtc, Guid? cursorId, int take, CancellationToken cancellationToken);

    /// <summary>Master panelin ciro/kazanç analizleri için; tüm firmalardaki başarılı tahsilatlar.</summary>
    Task<List<CompanyPayment>> GetAllSucceededAsync(CancellationToken cancellationToken);

    /// <summary>Master panelin "Ödeme İşlemleri" listesi için; tüm firmalardaki tüm ödemeler (başarılı+başarısız).</summary>
    Task<List<CompanyPayment>> GetPageAsync(
        CompanyPaymentStatus? status, DateTimeOffset? cursorOccurredAtUtc, Guid? cursorId, int take,
        CancellationToken cancellationToken);

    Task<Dictionary<CompanyPaymentStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken);

    Task<List<CompanyPayment>> GetAllByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken);

    void Add(CompanyPayment payment);

    void RemoveRange(IEnumerable<CompanyPayment> payments);
}
