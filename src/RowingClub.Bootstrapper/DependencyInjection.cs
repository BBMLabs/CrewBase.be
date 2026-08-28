using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RowingClub.BuildingBlocks.Application;
using RowingClub.BuildingBlocks.Infrastructure;
using RowingClub.BuildingBlocks.Observability;
using RowingClub.BuildingBlocks.Security;
using RowingClub.Identity.Application;
using RowingClub.Identity.Infrastructure;
using RowingClub.Scheduling.Infrastructure;

namespace RowingClub.Bootstrapper;

/// <summary>
/// The one place that knows about every module. RowingClub.Api calls
/// <see cref="AddRowingClubModules"/> and nothing else - it never registers a module's
/// Infrastructure services directly (spec section 4 solution yapısı: Api -> Bootstrapper only).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddRowingClubModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRowingClubSecurity(configuration);
        services.AddRowingClubInfrastructure(configuration);
        services.AddRowingClubObservability(configuration);

        services.AddRowingClubApplication(
            typeof(RowingClub.Identity.Application.DependencyInjection).Assembly,
            typeof(RowingClub.Scheduling.Application.Booking.BookAppointmentCommand).Assembly);

        services.AddIdentityApplication(configuration);
        services.AddIdentityInfrastructure(configuration);

        services.AddSchedulingInfrastructure(configuration);

        // Clubs, Memberships, Packages, Notifications, Reporting: scaffolded only,
        // no Application/Infrastructure registrations yet - see docs/ARCHITECTURE.md.

        return services;
    }
}
