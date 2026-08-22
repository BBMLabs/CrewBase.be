using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Scheduling.Domain.Customers;

/// <summary>
/// Firmanın üyesi/müşterisi. Tenant veritabanında yaşar; veritabanı zaten firmaya özel olduğu
/// için CompanyId taşımaz. Ad/telefon/e-posta kolonları veritabanında şifreli saklanır; telefon
/// ve e-posta aramaları blind index üzerinden yapılır. PasswordHash doluysa üye, kendi paneline
/// giriş yapabilen bir hesaptır (Argon2id hash - düz metin asla saklanmaz).
/// </summary>
public sealed class Customer
{
    public const int MaxLevel = 10;

    public Guid Id { get; private set; }

    public string FullName { get; private set; } = null!;

    public string Phone { get; private set; } = null!;

    public string? Email { get; private set; }

    /// <summary>Üye derecesi: 0 = derecesiz; 1..10 firma tanımlı dereceler. Seans gruplama anahtarıdır.</summary>
    public int Level { get; private set; }

    /// <summary>Üye paneli girişi için Argon2id hash; null = yalnızca telefonla randevu alan misafir kaydı.</summary>
    public string? PasswordHash { get; private set; }

    /// <summary>Üyenin kendi hatırlatma tercihi; null = firma varsayılanı, 0 = hatırlatma istemez.</summary>
    public int? DefaultReminderMinutes { get; private set; }

    /// <summary>
    /// Kulüp içinde BENZERSİZ üye kodu (ör. KRK-7M2XQ4). Arkadaş ekleme bu kodla yapılır;
    /// benzersizlik hem uygulama (çakışmada yeniden üretim) hem veritabanı (unique index) ile
    /// garanti edilir. Eski kayıtlar için null olabilir; ilk erişimde atanır.
    /// </summary>
    public string? MemberCode { get; private set; }

    public DateTimeOffset? EmailVerifiedAtUtc { get; private set; }

    public DateTimeOffset? PhoneVerifiedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Customer()
    {
    }

    public static Customer Create(string fullName, string phone, string? email) => new()
    {
        Id = Guid.NewGuid(),
        FullName = fullName.Trim(),
        Phone = phone.Trim(),
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(),
        Level = 0,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    public bool HasAccount => PasswordHash is not null;

    public void UpdateContact(string fullName, string? email)
    {
        FullName = fullName.Trim();
        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalized = email.Trim().ToLowerInvariant();
            if (normalized != Email)
            {
                Email = normalized;
                EmailVerifiedAtUtc = null; // e-posta değişti; yeniden doğrulanmalı
            }
        }
    }

    public void MarkVerified(VerificationPurpose purpose)
    {
        if (purpose == VerificationPurpose.Email)
            EmailVerifiedAtUtc = DateTimeOffset.UtcNow;
        else
            PhoneVerifiedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetLevel(int level)
    {
        if (level is < 0 or > MaxLevel)
            throw new DomainException("invalid_level", $"Derece 0-{MaxLevel} arasında olmalıdır.");

        Level = level;
    }

    public void AttachAccount(string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("email_required", "Üye hesabı için e-posta zorunludur.");

        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
    }

    public void AssignMemberCode(string code)
    {
        MemberCode = code;
    }

    public void SetDefaultReminder(int? minutes)
    {
        if (minutes is < 0 or > 10080)
            throw new DomainException("invalid_reminder", "Hatırlatma süresi 0-10080 dakika arasında olmalıdır.");

        DefaultReminderMinutes = minutes;
    }
}
