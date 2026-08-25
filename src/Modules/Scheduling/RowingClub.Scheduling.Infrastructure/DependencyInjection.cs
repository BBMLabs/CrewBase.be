using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RowingClub.BuildingBlocks.Application.Abstractions;
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
    public static IServiceCollection AddSchedulingInfrastructure(this IServiceCollection services)
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
        services.AddScoped<ITrainingSessionRepository, TrainingSessionRepository>();
        services.AddScoped<IBoatRepository, BoatRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IInstructorRepository, InstructorRepository>();
        services.AddScoped<ILessonPackageRepository, LessonPackageRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<ICustomerPackageRepository, CustomerPackageRepository>();
        services.AddScoped<IMemberLogRepository, MemberLogRepository>();
        services.AddScoped<IClosedDateRepository, ClosedDateRepository>();
        services.AddScoped<IFriendshipRepository, FriendshipRepository>();
        services.AddScoped<IDirectMessageRepository, DirectMessageRepository>();
        services.AddScoped<IConsentRecordRepository, ConsentRecordRepository>();
        services.AddScoped<IMembershipCardRepository, MembershipCardRepository>();
        services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
        services.AddScoped<ICommunityRepository, CommunityRepository>();
        services.AddScoped<ISchedulingUnitOfWork, SchedulingUnitOfWork>();

        return services;
    }
}
