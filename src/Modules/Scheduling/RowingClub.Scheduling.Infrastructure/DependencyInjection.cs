using Amazon.Runtime;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Scheduling.Application.Billing;
using RowingClub.Scheduling.Application.Files;
using RowingClub.Scheduling.Infrastructure.Billing.Iyzico;
using RowingClub.Scheduling.Infrastructure.Files;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Branches;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Cards;
using RowingClub.Scheduling.Domain.Community;
using RowingClub.Scheduling.Domain.Consents;
using RowingClub.Scheduling.Domain.Instructors;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Packages;
using RowingClub.Scheduling.Domain.Social;
using RowingClub.Scheduling.Domain.Sessions;
using RowingClub.Scheduling.Domain.Settings;
using RowingClub.Scheduling.Infrastructure.Persistence;
using RowingClub.Scheduling.Infrastructure.Repositories;

namespace RowingClub.Scheduling.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSchedulingInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<TenantConnectionStringFactory>();
        services.AddSingleton<ITenantDatabaseProvisioner, TenantDatabaseProvisioner>();

        // Bağlantı dizesi istek başına, API'nin doldurduğu ITenantDatabase'den kurulur. Tenant
        // çözülmeden bu context'i isteyen bir handler bilinçli olarak hata alır.
        services.AddDbContext<TenantDbContext>((provider, options) =>
        {
            // dotnet-ef araçları context keşfi sırasında bu factory'yi tenant bağlamı olmadan
            // çalıştırır; o durumda hiç açılmayacak sahte bir bağlantı verilir.
            if (EF.IsDesignTime)
            {
                options.UseNpgsql("Host=localhost;Database=tenant_design_time");
                return;
            }

            var tenantDatabase = provider.GetRequiredService<ITenantDatabase>();
            var connectionStringFactory = provider.GetRequiredService<TenantConnectionStringFactory>();
            options.UseNpgsql(connectionStringFactory.Create(tenantDatabase.DatabaseName));
        });

        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IMemberPasswordSetupTokenRepository, MemberPasswordSetupTokenRepository>();
        services.AddScoped<ITrainingSessionRepository, TrainingSessionRepository>();
        services.AddScoped<IBoatRepository, BoatRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IInstructorRepository, InstructorRepository>();
        services.AddScoped<ILessonPackageRepository, LessonPackageRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<ICustomerPackageRepository, CustomerPackageRepository>();
        services.AddScoped<IMemberLogRepository, MemberLogRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<IActivityLogWriter, ActivityLogWriter>();
        services.AddScoped<IBlockedIpAddressRepository, BlockedIpAddressRepository>();
        services.AddScoped<IBlockedIpChecker, BlockedIpChecker>();
        services.AddScoped<IClosedDateRepository, ClosedDateRepository>();
        services.AddScoped<IFriendshipRepository, FriendshipRepository>();
        services.AddScoped<IDirectMessageRepository, DirectMessageRepository>();
        services.AddScoped<IConsentRecordRepository, ConsentRecordRepository>();
        services.AddScoped<IMembershipCardRepository, MembershipCardRepository>();
        services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
        services.AddScoped<ICommunityRepository, CommunityRepository>();
        services.AddScoped<ISchedulingUnitOfWork, SchedulingUnitOfWork>();

        // Görsel depolama: R2_* env değişkenleri doluysa Cloudflare R2 (S3 uyumlu), boşsa yerel
        // disk kullanılır - IYZICO_*/SMTP_* ile aynı "boşsa devre dışı/varsayılana düş" deseni.
        // Bu bir DI-zamanı kararı olduğu için (hangi IFileStorageService implementasyonu
        // KAYDEDİLECEK), ham configuration'dan okunur - IOptions henüz kurulmamış olabilir.
        var r2Section = configuration.GetSection(R2StorageOptions.SectionName);
        var r2Configured =
            !string.IsNullOrWhiteSpace(r2Section[nameof(R2StorageOptions.AccountId)]) &&
            !string.IsNullOrWhiteSpace(r2Section[nameof(R2StorageOptions.AccessKeyId)]) &&
            !string.IsNullOrWhiteSpace(r2Section[nameof(R2StorageOptions.SecretAccessKey)]) &&
            !string.IsNullOrWhiteSpace(r2Section[nameof(R2StorageOptions.BucketName)]) &&
            !string.IsNullOrWhiteSpace(r2Section[nameof(R2StorageOptions.PublicBaseUrl)]);

        services.AddOptions<R2StorageOptions>().Bind(r2Section);

        if (r2Configured)
        {
            services.AddSingleton<IAmazonS3>(provider =>
            {
                var r2Options = provider.GetRequiredService<IOptions<R2StorageOptions>>().Value;
                var s3Config = new AmazonS3Config
                {
                    ServiceURL = $"https://{r2Options.AccountId}.r2.cloudflarestorage.com",
                    ForcePathStyle = true,
                    AuthenticationRegion = "auto",
                    // AWSSDK v4 varsayılan olarak "trailing checksum" akışlı gövde imzalama
                    // (...-PAYLOAD-TRAILER) dener; R2 bunu desteklemiyor ve isteği reddediyor
                    // ("STREAMING-AWS4-HMAC-SHA256-PAYLOAD-TRAILER not implemented"). Eski/standart
                    // imzalama biçimine (trailer'sız) geri dönmek için ikisini de WHEN_REQUIRED yap.
                    RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                    ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
                };
                return new AmazonS3Client(r2Options.AccessKeyId, r2Options.SecretAccessKey, s3Config);
            });
            services.AddSingleton<IFileStorageService, R2FileStorageService>();
        }
        else
        {
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        }

        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName));

        services.AddHttpClient<IIyzicoPaymentClient, IyzicoPaymentClient>(
            c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddOptions<PackagePaymentOptions>()
            .Bind(configuration.GetSection(PackagePaymentOptions.SectionName));

        return services;
    }
}
