using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RowingClub.BuildingBlocks.Security.Encryption;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Cards;
using RowingClub.Scheduling.Domain.Community;
using RowingClub.Scheduling.Domain.Consents;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Instructors;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Packages;
using RowingClub.Scheduling.Domain.Sessions;
using RowingClub.Scheduling.Domain.Settings;
using RowingClub.Scheduling.Domain.Social;

namespace RowingClub.Scheduling.Infrastructure.Persistence;

/// <summary>
/// Her firmanın KENDİ veritabanına açılan context (database-per-tenant). Bağlantı dizesi, isteği
/// yapan firmaya göre ITenantDatabase üzerinden kurulur - bkz. DependencyInjection.
///
/// Kişisel veriler (üye adı/telefonu/e-postası, eğitmen iletişim bilgileri, randevu notu)
/// veritabanında AES-256-GCM ile ŞİFRELİ saklanır; telefon araması, düz metin saklamadan,
/// HMAC blind index kolonu üzerinden yapılır. Şifreleyiciler singleton olduğu için EF'in
/// context-tipi başına model önbelleği ile uyumludur.
/// </summary>
public sealed class TenantDbContext(
    DbContextOptions<TenantDbContext> options,
    IFieldEncryptor fieldEncryptor,
    IBlindIndexer blindIndexer) : DbContext(options)
{
    public const string PhoneIndexColumn = "PhoneIndex";

    public const string EmailIndexColumn = "EmailIndex";

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();

    public DbSet<Boat> Boats => Set<Boat>();

    public DbSet<Instructor> Instructors => Set<Instructor>();

    public DbSet<LessonPackage> LessonPackages => Set<LessonPackage>();

    public DbSet<CompanySettings> Settings => Set<CompanySettings>();

    public DbSet<CustomerPackage> CustomerPackages => Set<CustomerPackage>();

    public DbSet<MemberLog> MemberLogs => Set<MemberLog>();

    public DbSet<ClosedDate> ClosedDates => Set<ClosedDate>();

    public DbSet<Friendship> Friendships => Set<Friendship>();

    public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();

    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();

    public DbSet<MembershipCard> MembershipCards => Set<MembershipCard>();

    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();

    public DbSet<Post> Posts => Set<Post>();

    public DbSet<PostLike> PostLikes => Set<PostLike>();

    public DbSet<PostComment> PostComments => Set<PostComment>();

    public DbSet<EventParticipation> EventParticipations => Set<EventParticipation>();

    public DbSet<Follow> Follows => Set<Follow>();

    public static string NormalizePhone(string phone) =>
        new(phone.Where(char.IsDigit).ToArray());

    public string ComputePhoneIndex(string phone) =>
        blindIndexer.ComputeBlindIndex(NormalizePhone(phone));

    public string ComputeEmailIndex(string email) =>
        blindIndexer.ComputeBlindIndex(email.Trim().ToLowerInvariant());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var encrypted = new ValueConverter<string, string>(
            plain => fieldEncryptor.Encrypt(plain).Serialize(),
            stored => fieldEncryptor.Decrypt(EncryptedValue.Deserialize(stored)));

        modelBuilder.Entity<Customer>(builder =>
        {
            builder.ToTable("customers");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.FullName).HasConversion(encrypted).IsRequired();
            builder.Property(c => c.Phone).HasConversion(encrypted).IsRequired();
            builder.Property(c => c.Email).HasConversion(encrypted!);
            builder.Property(c => c.Level);
            builder.Property(c => c.PasswordHash).HasMaxLength(500);
            builder.Property(c => c.DefaultReminderMinutes);

            // Şifreli telefon/e-posta kolonları aranamaz; benzersizlik ve arama blind index üzerinden.
            builder.Property<string>(PhoneIndexColumn).HasMaxLength(128).IsRequired();
            builder.HasIndex(PhoneIndexColumn).IsUnique();

            builder.Property<string?>(EmailIndexColumn).HasMaxLength(128);
            builder.HasIndex(EmailIndexColumn).IsUnique().HasFilter($"\"{EmailIndexColumn}\" IS NOT NULL");

            // Üye kodu paylaşılmak İÇİN üretilir (gizli veri değildir); benzersizliği DB garantiler.
            builder.Property(c => c.MemberCode).HasMaxLength(12);
            builder.HasIndex(c => c.MemberCode).IsUnique().HasFilter("\"MemberCode\" IS NOT NULL");
        });

        modelBuilder.Entity<Friendship>(builder =>
        {
            builder.ToTable("friendships");
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);
            builder.HasIndex(f => new { f.RequesterId, f.AddresseeId }).IsUnique();
            builder.HasIndex(f => f.AddresseeId);

            builder.HasOne<Customer>().WithMany().HasForeignKey(f => f.RequesterId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Customer>().WithMany().HasForeignKey(f => f.AddresseeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DirectMessage>(builder =>
        {
            builder.ToTable("direct_messages");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Body).HasConversion(encrypted).IsRequired();
            builder.HasIndex(m => new { m.SenderId, m.RecipientId, m.SentAtUtc });
            builder.HasIndex(m => new { m.RecipientId, m.ReadAtUtc });

            builder.HasOne<Customer>().WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Customer>().WithMany().HasForeignKey(m => m.RecipientId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConsentRecord>(builder =>
        {
            builder.ToTable("consent_records");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.ConsentKey).HasMaxLength(40).IsRequired();
            builder.Property(c => c.IpAddress).HasConversion(encrypted!);
            builder.HasIndex(c => new { c.CustomerId, c.ConsentKey, c.AcceptedAtUtc });

            builder.HasOne<Customer>().WithMany().HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MembershipCard>(builder =>
        {
            builder.ToTable("membership_cards");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(20);
            builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(c => c.CardNumber).HasConversion(encrypted!);
            builder.Property(c => c.CompanyName).HasMaxLength(200).IsRequired();
            builder.Property(c => c.PhotoBase64).HasConversion(encrypted!);
            builder.Property(c => c.PhotoContentType).HasMaxLength(40);
            builder.HasIndex(c => new { c.CustomerId, c.Type }).IsUnique();

            builder.HasOne<Customer>().WithMany().HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Post>(builder =>
        {
            builder.ToTable("posts");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Body).HasConversion(encrypted).IsRequired();
            builder.Property(p => p.MediaKind).HasConversion<string>().HasMaxLength(10);
            builder.Property(p => p.MediaContentType).HasMaxLength(40);
            builder.Property(p => p.EventTitle).HasMaxLength(200);
            builder.HasIndex(p => p.CreatedAtUtc);

            builder.HasOne<Customer>().WithMany().HasForeignKey(p => p.AuthorCustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostMedia>(builder =>
        {
            builder.ToTable("post_media");
            builder.HasKey(m => m.PostId);
            builder.Property(m => m.Base64).HasConversion(encrypted).IsRequired();
            builder.Property(m => m.ContentType).HasMaxLength(40).IsRequired();
            builder.HasOne<Post>().WithOne().HasForeignKey<PostMedia>(m => m.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostLike>(builder =>
        {
            builder.ToTable("post_likes");
            builder.HasKey(l => new { l.PostId, l.CustomerId });
            builder.HasOne<Post>().WithMany().HasForeignKey(l => l.PostId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Customer>().WithMany().HasForeignKey(l => l.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostComment>(builder =>
        {
            builder.ToTable("post_comments");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Body).HasConversion(encrypted).IsRequired();
            builder.HasIndex(c => new { c.PostId, c.CreatedAtUtc });
            builder.HasOne<Post>().WithMany().HasForeignKey(c => c.PostId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Customer>().WithMany().HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EventParticipation>(builder =>
        {
            builder.ToTable("event_participations");
            builder.HasKey(p => new { p.PostId, p.CustomerId });
            builder.HasOne<Post>().WithMany().HasForeignKey(p => p.PostId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Customer>().WithMany().HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Follow>(builder =>
        {
            builder.ToTable("follows");
            builder.HasKey(f => new { f.FollowerId, f.FollowedId });
            builder.HasIndex(f => f.FollowedId);
            builder.HasOne<Customer>().WithMany().HasForeignKey(f => f.FollowerId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Customer>().WithMany().HasForeignKey(f => f.FollowedId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VerificationCode>(builder =>
        {
            builder.ToTable("verification_codes");
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Purpose).HasConversion<string>().HasMaxLength(10);
            builder.Property(v => v.CodeHash).HasMaxLength(200).IsRequired();
            builder.HasIndex(v => new { v.CustomerId, v.Purpose });

            builder.HasOne<Customer>().WithMany().HasForeignKey(v => v.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerPackage>(builder =>
        {
            builder.ToTable("customer_packages");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.PackageName).HasMaxLength(200).IsRequired();
            builder.HasIndex(p => p.CustomerId);

            builder.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<LessonPackage>()
                .WithMany()
                .HasForeignKey(p => p.LessonPackageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MemberLog>(builder =>
        {
            builder.ToTable("member_logs");
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Event).HasMaxLength(60).IsRequired();
            builder.Property(l => l.Details).HasConversion(encrypted!);
            builder.HasIndex(l => new { l.CustomerId, l.AtUtc });

            builder.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(l => l.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClosedDate>(builder =>
        {
            builder.ToTable("closed_dates");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Reason).HasMaxLength(300);
            builder.HasIndex(c => c.Date).IsUnique();
        });

        modelBuilder.Entity<Instructor>(builder =>
        {
            builder.ToTable("instructors");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.FullName).HasMaxLength(200).IsRequired();
            builder.Property(i => i.Phone).HasConversion(encrypted!);
            builder.Property(i => i.Email).HasConversion(encrypted!);
        });

        modelBuilder.Entity<Boat>(builder =>
        {
            builder.ToTable("boats");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
            builder.Property(b => b.Class).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<LessonPackage>(builder =>
        {
            builder.ToTable("lesson_packages");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
            builder.Property(p => p.Description).HasMaxLength(1000);
            builder.Property(p => p.Price).HasPrecision(12, 2);
        });

        modelBuilder.Entity<CompanySettings>(builder =>
        {
            builder.ToTable("company_settings");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.ReminderOptionsMinutes).HasMaxLength(200).IsRequired();
            builder.Property(s => s.TimeZoneId).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<TrainingSession>(builder =>
        {
            builder.ToTable("training_sessions");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.BoatClass).HasConversion<string>().HasMaxLength(20);

            builder.HasOne(s => s.Boat)
                .WithMany()
                .HasForeignKey(s => s.BoatId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(s => s.Instructor)
                .WithMany()
                .HasForeignKey(s => s.InstructorId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(s => new { s.Date, s.StartTime });
        });

        modelBuilder.Entity<Appointment>(builder =>
        {
            builder.ToTable("appointments");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Note).HasConversion(encrypted!);
            builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

            builder.HasOne(a => a.Session)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.Customer)
                .WithMany()
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<CustomerPackage>()
                .WithMany()
                .HasForeignKey(a => a.CustomerPackageId)
                .OnDelete(DeleteBehavior.SetNull);

            // Aynı üye aynı seansa iki kez yazılamaz (iptaller hariç).
            builder.HasIndex(a => new { a.SessionId, a.CustomerId })
                .IsUnique()
                .HasFilter("\"Status\" <> 'Cancelled'");

            builder.HasIndex(a => new { a.Date, a.StartTime });
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Telefon blind index'i her ekleme/güncellemede yeniden hesaplanır; düz metin telefon
        // hiçbir kolonda saklanmaz.
        foreach (var entry in ChangeTracker.Entries<Customer>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Property<string>(PhoneIndexColumn).CurrentValue = ComputePhoneIndex(entry.Entity.Phone);
                entry.Property<string?>(EmailIndexColumn).CurrentValue =
                    entry.Entity.Email is null ? null : ComputeEmailIndex(entry.Entity.Email);
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
