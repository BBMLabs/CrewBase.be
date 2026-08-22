using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Scheduling.Domain.Customers;

public enum VerificationPurpose
{
    Email = 0,
    Phone = 1,
}

/// <summary>
/// Üye e-posta/telefon doğrulaması için 6 haneli OTP kaydı. Kod düz metin saklanmaz (hash);
/// 10 dakika geçerlidir ve 5 hatalı denemeden sonra kilitlenir.
/// </summary>
public sealed class VerificationCode
{
    public const int MaxAttempts = 5;

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public VerificationPurpose Purpose { get; private set; }

    public string CodeHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset? UsedAtUtc { get; private set; }

    private VerificationCode()
    {
    }

    public static VerificationCode Issue(Guid customerId, VerificationPurpose purpose, string codeHash) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        Purpose = purpose,
        CodeHash = codeHash,
        ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10),
    };

    public bool CanAttempt => UsedAtUtc is null && Attempts < MaxAttempts && ExpiresAtUtc > DateTimeOffset.UtcNow;

    public bool TryConsume(string codeHash)
    {
        if (!CanAttempt)
            throw new DomainException("otp_expired", "Kodun süresi dolmuş veya deneme hakkı bitmiş. Yeni kod isteyin.");

        Attempts++;

        if (!string.Equals(CodeHash, codeHash, StringComparison.Ordinal))
            return false;

        UsedAtUtc = DateTimeOffset.UtcNow;
        return true;
    }
}

public interface IVerificationCodeRepository
{
    Task<VerificationCode?> GetActiveAsync(
        Guid customerId, VerificationPurpose purpose, CancellationToken cancellationToken);

    void Add(VerificationCode code);

    void Remove(VerificationCode code);
}
