using Microsoft.Extensions.FileProviders;
using RowingClub.Api.Configuration;
using RowingClub.Api.Endpoints;
using RowingClub.Api.HealthChecks;
using RowingClub.Api.RateLimiting;
using RowingClub.Api.Security;
using RowingClub.Api.Tenancy;
using RowingClub.Api.Versioning;
using RowingClub.Bootstrapper;
using RowingClub.BuildingBlocks.Infrastructure.Configuration;
using RowingClub.BuildingBlocks.Observability;
using RowingClub.BuildingBlocks.Observability.Logging;
using Scalar.AspNetCore;

DotEnvFileLoader.LoadForEnvironment(Environment.GetEnvironmentVariable("APP_ENV")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddRowingClubEnvironmentMapping();
builder.AddRowingClubSerilog();

builder.Services
    .AddRowingClubModules(builder.Configuration)
    .AddRowingClubAuthentication(builder.Configuration)
    .AddRowingClubApiVersioning()
    .AddRowingClubRateLimiting()
    .AddRowingClubHealthChecks()
    .AddOpenApi();

builder.Services.AddScoped<TenantResolver>();
builder.Services.AddScoped<RowingClub.Scheduling.Application.Reminders.IAppointmentReminderSender,
    EmailAppointmentReminderSender>();
builder.Services.AddScoped<RowingClub.Scheduling.Application.Reminders.IPackageExpiryReminderSender,
    EmailPackageExpiryReminderSender>();
builder.Services.AddScoped<RowingClub.Scheduling.Application.Members.IMemberWelcomeEmailSender,
    EmailMemberWelcomeSender>();
builder.Services.AddScoped<RowingClub.Scheduling.Application.Members.IMemberBranchTransferEmailSender,
    EmailMemberBranchTransferSender>();
builder.Services.AddSingleton<MemberTokenIssuer>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, TenantUserIdProvider>();
builder.Services.AddScoped<RowingClub.Scheduling.Application.Members.IChatNotifier, SignalRChatNotifier>();
builder.Services.AddScoped<RowingClub.Scheduling.Application.Members.IOtpSender, EmailOtpSender>();
builder.Services.AddHostedService<TenantMigrationHostedService>();
builder.Services.AddHostedService<PlatformAdminSeeder>();
builder.Services.AddHostedService<ReminderWorker>();
builder.Services.AddHostedService<SubscriptionSafetyNetWorker>();

// React frontend'i (Vite dev sunucusu) ayrı origin'den çalışır; *.localhost subdomain'leri ve
// mock domain için CORS açılır. Kimlik Authorization header'ıyla taşındığından cookie yoktur.
builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
    policy.SetIsOriginAllowed(origin =>
            Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
            (uri.Host == "localhost" || uri.Host.EndsWith(".localhost", StringComparison.Ordinal) ||
             uri.Host.EndsWith(".faturebase.com", StringComparison.Ordinal)))
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials())); // SignalR JS istemcisi negotiate isteğini credentials ile atar

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    if (!app.Environment.IsDevelopment())
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    await next();
});

app.UseRowingClubObservability();
app.UseCors("frontend");

// Yüklenen dosyalar (paket görselleri vb.) - kimlik doğrulaması gerektirmez, bir kartın üzerinde
// gösterilecek herkese açık pazarlama görselidir. UPLOADS_ROOT_PATH env değişkeninden (varsayılan
// ./uploads) LocalFileStorageService ile aynı klasörü "/uploads" altında sunar.
var uploadsRootPath = Path.GetFullPath(app.Configuration["FileStorage:RootPath"] ?? "./uploads");
Directory.CreateDirectory(uploadsRootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRootPath),
    RequestPath = "/uploads",
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapPublicSiteEndpoints();
app.MapCompanyPanelEndpoints();
app.MapMemberEndpoints();
app.MapWebhookEndpoints();
app.MapHub<ChatHub>("/hubs/chat").RequireCors("frontend");
app.MapRowingClubHealthChecks();

app.Run();

namespace RowingClub.Api
{
    /// <summary>Entry point marker so WebApplicationFactory&lt;Program&gt; can target this assembly in tests.</summary>
    public partial class Program;
    //selam
}
