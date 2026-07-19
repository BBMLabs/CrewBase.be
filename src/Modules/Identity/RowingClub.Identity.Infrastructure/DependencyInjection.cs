using Microsoft.Extensions.DependencyInjection;
using RowingClub.BuildingBlocks.Infrastructure.Mongo.Migrations;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Infrastructure.Persistence;
using RowingClub.Identity.Infrastructure.Repositories;

namespace RowingClub.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, MongoUserRepository>();
        services.AddScoped<ICredentialRepository, MongoCredentialRepository>();
        services.AddScoped<IRefreshTokenRepository, MongoRefreshTokenRepository>();
        services.AddScoped<IUserSessionRepository, MongoUserSessionRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, MongoEmailVerificationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, MongoPasswordResetTokenRepository>();

        services.AddSingleton<IMongoMigration, IdentityCollectionsMigration>();

        return services;
    }
}
