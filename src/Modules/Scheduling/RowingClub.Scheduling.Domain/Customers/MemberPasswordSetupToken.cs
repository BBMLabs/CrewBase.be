using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Scheduling.Domain.Customers;

/// <summary>
/// Firma panelinden e-postalı eklenen bir üyenin ilk şifresini kendisinin belirlemesi için
/// gönderilen tek kullanımlık bağlantının token'ı. Ham token yalnızca e-postada gider; burada
/// yalnızca hash'i tutulur (parola sıfırlama token'larıyla aynı desen).
/// </summary>
public sealed class MemberPasswordSetupToken
{
    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? UsedAtUtc { get; private set; }

    private MemberPasswordSetupToken()
    {
    }

    public static MemberPasswordSetupToken Issue(Guid customerId, string tokenHash, TimeSpan lifetime) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        TokenHash = tokenHash,
        ExpiresAtUtc = DateTimeOffset.UtcNow.Add(lifetime),
    };

    public bool IsValid => UsedAtUtc is null && ExpiresAtUtc > DateTimeOffset.UtcNow;

    public void MarkUsed()
    {
        if (!IsValid)
            throw new DomainException("password_setup_token_invalid", "Şifre oluşturma bağlantısı geçersiz veya süresi dolmuş.");

        UsedAtUtc = DateTimeOffset.UtcNow;
    }
}
