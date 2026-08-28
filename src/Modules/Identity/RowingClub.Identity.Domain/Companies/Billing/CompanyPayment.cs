using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Identity.Domain.Companies.Billing;

public enum CompanyPaymentKind
{
    InitialSubscription = 0,
    Upgrade = 1,
    SubscriptionRenewal = 2,
}

public enum CompanyPaymentStatus
{
    Succeeded = 0,
    Failed = 1,
}

/// <summary>
/// Tek bir tahsilat girişi (append-only). Her satır o anki paketi de taşıdığı için firma panelindeki
/// "Ödeme Geçmişi" hem de "Paket Geçmişi" ihtiyacını tek tablodan karşılar.
/// </summary>
public sealed class CompanyPayment : AggregateRoot<Guid>
{
    public Guid CompanyId { get; private set; }

    public CompanyPlan Plan { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = "TRY";

    public CompanyPaymentKind Kind { get; private set; }

    public CompanyPaymentStatus Status { get; private set; }

    /// <summary>
    /// iyzico'nun tahsilat referans kodu. Webhook tekrarına (replay) karşı benzersizlik burada
    /// zorlanır (unique index, bkz. CompanyPaymentConfiguration) - tekrar gelen aynı kod bir daha
    /// satır eklemez.
    /// </summary>
    public string? IyzicoPaymentReferenceCode { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    private CompanyPayment()
    {
    }

    private CompanyPayment(
        Guid id, Guid companyId, CompanyPlan plan, decimal amount, string currency,
        CompanyPaymentKind kind, CompanyPaymentStatus status,
        string? iyzicoPaymentReferenceCode, string? failureReason)
        : base(id)
    {
        CompanyId = companyId;
        Plan = plan;
        Amount = amount;
        Currency = currency;
        Kind = kind;
        Status = status;
        IyzicoPaymentReferenceCode = iyzicoPaymentReferenceCode;
        FailureReason = failureReason;
        OccurredAtUtc = DateTimeOffset.UtcNow;
    }

    public static CompanyPayment Succeeded(
        Guid companyId, CompanyPlan plan, decimal amount, string currency,
        CompanyPaymentKind kind, string iyzicoPaymentReferenceCode) =>
        new(Guid.NewGuid(), companyId, plan, amount, currency, kind,
            CompanyPaymentStatus.Succeeded, iyzicoPaymentReferenceCode, failureReason: null);

    public static CompanyPayment Failed(
        Guid companyId, CompanyPlan plan, decimal amount, string currency,
        CompanyPaymentKind kind, string? iyzicoPaymentReferenceCode, string failureReason) =>
        new(Guid.NewGuid(), companyId, plan, amount, currency, kind,
            CompanyPaymentStatus.Failed, iyzicoPaymentReferenceCode, failureReason);
}
