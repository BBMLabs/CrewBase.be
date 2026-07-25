using RowingClub.Api.Configuration;
using RowingClub.Api.Endpoints;
using RowingClub.Api.HealthChecks;
using RowingClub.Api.RateLimiting;
using RowingClub.Api.Security;
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseRowingClubObservability();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapRowingClubHealthChecks();

app.Run();

namespace RowingClub.Api
{
    /// <summary>Entry point marker so WebApplicationFactory&lt;Program&gt; can target this assembly in tests.</summary>
    public partial class Program;
}
