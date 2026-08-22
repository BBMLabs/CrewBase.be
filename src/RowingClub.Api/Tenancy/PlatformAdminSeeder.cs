using MediatR;
using RowingClub.Identity.Application.Platform;

namespace RowingClub.Api.Tenancy;

/// <summary>
/// Açılışta PLATFORM_ADMIN_EMAIL / PLATFORM_ADMIN_PASSWORD env değişkenlerinden master admin
/// hesabını garantiler. Env yoksa hiçbir şey yapmaz; şifre yalnızca hash'lenerek saklanır.
/// </summary>
public sealed class PlatformAdminSeeder(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<PlatformAdminSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var email = configuration["PLATFORM_ADMIN_EMAIL"];
        var password = configuration["PLATFORM_ADMIN_PASSWORD"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(new EnsurePlatformAdminCommand(email, password), cancellationToken);
            logger.LogInformation("Platform admin hesabı hazır: {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Platform admin hesabı oluşturulamadı.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
