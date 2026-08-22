namespace RowingClub.Scheduling.Domain.Consents;

/// <summary>
/// Bir üyenin/misafirin beyan onayı: tarih + IP ile hukuki iz bırakır. Misafirler her randevuda
/// yeniden onayladığı için aynı anahtar birden çok kez kayıtlanabilir; sorgular en güncelini alır.
/// Accepted=false, isteğe bağlı rızanın geri çekildiğini gösterir.
/// </summary>
public sealed class ConsentRecord
{
    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string ConsentKey { get; private set; } = null!;

    public bool Accepted { get; private set; }

    public DateTimeOffset AcceptedAtUtc { get; private set; }

    /// <summary>Onayın verildiği IP adresi (şifreli saklanır - kişisel veridir).</summary>
    public string? IpAddress { get; private set; }

    private ConsentRecord()
    {
    }

    public static ConsentRecord Create(Guid customerId, string consentKey, bool accepted, string? ipAddress) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        ConsentKey = consentKey,
        Accepted = accepted,
        AcceptedAtUtc = DateTimeOffset.UtcNow,
        IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
    };
}

public interface IConsentRecordRepository
{
    /// <summary>Üyenin her beyan anahtarı için EN GÜNCEL kaydı.</summary>
    Task<Dictionary<string, ConsentRecord>> GetLatestByCustomerAsync(
        Guid customerId, CancellationToken cancellationToken);

    void Add(ConsentRecord record);
}
