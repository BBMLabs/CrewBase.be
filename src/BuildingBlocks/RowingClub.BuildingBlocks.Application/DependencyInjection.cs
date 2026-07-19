using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RowingClub.BuildingBlocks.Application.Behaviors;

namespace RowingClub.BuildingBlocks.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR against the given module assemblies plus the shared pipeline behaviors,
    /// in the fixed order required by spec section 5: logging -> performance -> validation ->
    /// idempotency -> transaction (transaction must run last so it wraps the actual handler call
    /// with nothing left to fail afterwards).
    /// </summary>
    public static IServiceCollection AddRowingClubApplication(
        this IServiceCollection services,
        params Assembly[] moduleApplicationAssemblies)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(moduleApplicationAssemblies);

            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(IdempotencyBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        foreach (var assembly in moduleApplicationAssemblies)
        {
            services.AddValidatorsFromAssembly(assembly);
        }

        return services;
    }
}
