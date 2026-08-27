using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using RowingClub.Api.Security;

namespace RowingClub.Api.RateLimiting;

public static class RateLimitingSetup
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddRowingClubRateLimiting(this IServiceCollection services)
    {
        var authPermitLimit = GetPermitLimitFromEnv();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(AuthPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ClientIp.Resolve(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        return services;
    }

    private static int GetPermitLimitFromEnv()
    {
        var envValue = Environment.GetEnvironmentVariable("AUTH_RATE_LIMIT");
        return int.TryParse(envValue, out var limit) ? limit : 10;
    }
}
