using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;

#nullable disable

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres.Migrations
{
    [DbContext(typeof(RowingClubDbContext))]
    [Migration("20260924203304_AddCompanySeoSettings")]
    partial class AddCompanySeoSettings
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("RowingClub.BuildingBlocks.Infrastructure.Outbox.InboxMessage", b =>
                {
                    b.Property<Guid>("EventId")
                        .HasColumnType("uuid");

                    b.Property<string>("ConsumerName")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("ProcessedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("EventId", "ConsumerName");

                    b.ToTable("inbox_messages", (string)null);
                });

            modelBuilder.Entity("RowingClub.BuildingBlocks.Infrastructure.Outbox.OutboxMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("CorrelationId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("LastError")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("OccurredOnUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset?>("ProcessedOnUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("RetryCount")
                        .HasColumnType("integer");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ProcessedOnUtc");

                    b.ToTable("outbox_messages", (string)null);
                });

            modelBuilder.Entity("RowingClub.Identity.Domain.Companies.Billing.CompanyPayment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("Amount")
                        .HasColumnType("numeric(10,2)");

                    b.Property<Guid>("CompanyId")
                        .HasColumnType("uuid");

                    b.Property<string>("Currency")
                        .IsRequired()
                        .HasMaxLength(3)
                        .HasColumnType("character varying(3)");

                    b.Property<string>("FailureReason")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("IyzicoPaymentReferenceCode")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("Kind")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("character varying(30)");

                    b.Property<DateTimeOffset>("OccurredAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Plan")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("CompanyId");

                    b.HasIndex("IyzicoPaymentReferenceCode")
                        .IsUnique();

                    b.ToTable("identity_company_payments", (string)null);
                });

            modelBuilder.Entity("RowingClub.Identity.Domain.Companies.Billing.CompanySubscription", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("CompanyId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset?>("CurrentPeriodEndUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("IyzicoCustomerReferenceCode")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("IyzicoSubscriptionReferenceCode")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("PendingPlan")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<DateTimeOffset?>("PendingPlanEffectiveAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<DateTimeOffset>("UpdatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("CompanyId")
                        .IsUnique();

                    b.HasIndex("IyzicoSubscriptionReferenceCode");

                    b.ToTable("identity_company_subscriptions", (string)null);
                });

            modelBuilder.Entity("RowingClub.Identity.Domain.Companies.Company", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("AboutText")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<string>("Address")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<bool>("AllowIndexing")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset?>("ApprovedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid?>("ApprovedByUserId")
                        .HasColumnType("uuid");

                    b.Property<string>("ContactEmail")
                        .HasMaxLength(254)
                        .HasColumnType("character varying(254)");

                    b.Property<DateTimeOffset>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool?>("CustomCanExportData")
                        .HasColumnType("boolean");

                    b.Property<bool?>("CustomHasAdvancedReports")
                        .HasColumnType("boolean");

                    b.Property<bool?>("CustomHasAutomaticDuesReminders")
                        .HasColumnType("boolean");

                    b.Property<int?>("CustomMaxBoats")
                        .HasColumnType("integer");

                    b.Property<int?>("CustomMaxBranches")
                        .HasColumnType("integer");

                    b.Property<int?>("CustomMaxEmployees")
                        .HasColumnType("integer");

                    b.Property<int?>("CustomMaxInstructors")
                        .HasColumnType("integer");

                    b.Property<int?>("CustomMaxManagers")
                        .HasColumnType("integer");

                    b.Property<int?>("CustomMaxMembers")
                        .HasColumnType("integer");

                    b.Property<string>("DatabaseName")
                        .IsRequired()
                        .HasMaxLength(63)
                        .HasColumnType("character varying(63)");

                    b.Property<DateTimeOffset?>("DeletedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("FacebookUrl")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("GoogleAnalyticsId")
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)");

                    b.Property<string>("GoogleMapsUrl")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("GoogleSiteVerification")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("InstagramUrl")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<string>("LinkedinUrl")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("LogoPath")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<string>("PinterestUrl")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("Plan")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("SeoDescription")
                        .HasMaxLength(170)
                        .HasColumnType("character varying(170)");

                    b.Property<string>("SeoKeywords")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("SeoTitle")
                        .HasMaxLength(70)
                        .HasColumnType("character varying(70)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("Subdomain")
                        .IsRequired()
                        .HasMaxLength(63)
                        .HasColumnType("character varying(63)");

                    b.Property<string>("Tagline")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("TaxNumber")
                        .HasMaxLength(11)
                        .HasColumnType("character varying(11)");

                    b.Property<string>("TelegramUrl")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.Property<string>("WhatsappUrl")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("XUrl")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("YoutubeUrl")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.HasIndex("Subdomain")
                        .IsUnique();

                    b.ToTable("identity_companies", (string)null);
                });

            modelBuilder.Entity("RowingClub.Identity.Domain.Companies.CompanyGalleryImage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("CompanyId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("ImagePath")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.HasKey("Id");

                    b.HasIndex("CompanyId");

                    b.ToTable("identity_company_gallery_images", (string)null);
                });

            modelBuilder.Entity("RowingClub.Identity.Domain.Platform.PlatformActivityLog", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("ActorEmail")
                        .IsRequired()
                        .HasMaxLength(254)
                        .HasColumnType("character varying(254)");

                    b.Property<Guid>("ActorUserId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("AtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("IpAddress")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<Guid?>("TargetCompanyId")
                        .HasColumnType("uuid");

                    b.Property<string>("UserAgent")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.HasKey("Id");

                    b.HasIndex("AtUtc");

                    b.HasIndex("TargetCompanyId");

                    b.ToTable("identity_platform_activity_logs", (string)null);
                });

            modelBuilder.Entity("RowingClub.Identity.Domain.Tokens.EmailVerificationToken", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("ExpiresAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("TokenHash")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset?>("UsedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("TokenHash")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("identity_email_verification_tokens", (string)null);
                });

            modelBuilder.Entity("RowingClub.Identity.Domain.Tokens.PasswordResetToken", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("ExpiresAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("TokenHash")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset?>("UsedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("TokenHash")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("identity_password_reset_tokens", (string)null);
                });

            modelBuilder.Entity("RowingClub.Identity.Domain.Tokens.PendingTwoFactorToken", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("DeviceInfo")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<DateTimeOffset>("ExpiresAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsUsed")
                        .HasColumnType("boolean");

                    b.Property<string>("TokenHash")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("TokenHash")
                        .IsUnique();

                    b.ToTable("identity_pending_two_factor_tokens", (string)null);
                });

            modelBuilder.Entity("RowingClub.Identity.Domain.Tokens.RefreshToken", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset>("ExpiresAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("FamilyId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("ReplacedByTokenId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset?>("RevokedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("TokenHash")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("FamilyId");

                    b.HasIndex("TokenHash")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("identity_refresh_tokens", (string)null);
                });

            modelBuilder.Entity("RowingClub.Identity.Domain.Tokens.UserSession", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("DeviceInfo")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("LastSeenAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("RefreshTokenFamilyId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset?>("RevokedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("RefreshTokenFamilyId")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("identity_user_sessions", (string)null);
                });

            modelBuilder.Entity("RowingClub.Identity.Domain.Users.Credential", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset?>("ChangedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("identity_credentials", (string)null);
                });

            modelBuilder.Entity("RowingClub.Identity.Domain.Users.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid?>("CompanyId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("EmailVerified")
                        .HasColumnType("boolean");

                    b.Property<int>("FailedLoginAttemptCount")
                        .HasColumnType("integer");

                    b.Property<DateTimeOffset?>("LastLoginAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset?>("LockedUntilUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("boolean");

                    b.Property<string>("TwoFactorMethod")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasDefaultValue("None");

                    b.Property<string>("TwoFactorSecret")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<long>("Version")
                        .IsConcurrencyToken()
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("CompanyId");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.ToTable("identity_users", (string)null);
                });

            modelBuilder.Entity("RowingClub.Identity.Domain.Users.UserRecoveryCode", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("CodeHash")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<DateTimeOffset>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsUsed")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset?>("UsedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "IsUsed");

                    b.ToTable("identity_recovery_codes", (string)null);
                });

            modelBuilder.Entity("RowingClub.Identity.Domain.Companies.CompanyGalleryImage", b =>
                {
                    b.HasOne("RowingClub.Identity.Domain.Companies.Company", null)
                        .WithMany()
                        .HasForeignKey("CompanyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
