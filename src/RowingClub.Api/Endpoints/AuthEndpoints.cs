using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RowingClub.Api.RateLimiting;
using RowingClub.Identity.Application.Login;
using RowingClub.Identity.Application.Logout;
using RowingClub.Identity.Application.LogoutAll;
using RowingClub.Identity.Application.Refresh;
using RowingClub.Identity.Application.Register;

namespace RowingClub.Api.Endpoints;

public sealed record RegisterRequest(string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

/// <summary>Maps <c>/api/v1/auth/*</c> - see spec section 14 "Auth" and docs/API_ENDPOINTS.md.</summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v1/auth")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(1)
            .WithTags("Auth")
            .RequireRateLimiting(RateLimitingSetup.AuthPolicy);

        group.MapPost("/register", RegisterAsync)
            .WithName("RegisterUser")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshAccessToken")
            .Produces<RefreshTokenResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        group.MapPost("/logout-all", LogoutAllAsync)
            .WithName("LogoutAllDevices")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new RegisterCommand(request.Email, request.Password), cancellationToken);
        return Results.Created($"/api/v1/me", response);
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
    {
        var deviceInfo = httpContext.Request.Headers.UserAgent.ToString();
        var response = await sender.Send(
            new LoginCommand(request.Email, request.Password, deviceInfo), cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> LogoutAsync(
        [FromBody] LogoutRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> LogoutAllAsync(ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutAllCommand(), cancellationToken);
        return Results.NoContent();
    }
}
