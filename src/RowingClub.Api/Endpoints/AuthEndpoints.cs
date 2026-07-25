using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RowingClub.Api.RateLimiting;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Identity.Application.Companies.RegisterCompany;
using RowingClub.Identity.Application.EmailVerification;
using RowingClub.Identity.Application.Login;
using RowingClub.Identity.Application.Logout;
using RowingClub.Identity.Application.LogoutAll;
using RowingClub.Identity.Application.PasswordReset;
using RowingClub.Identity.Application.Refresh;
using RowingClub.Identity.Application.Register;
using RowingClub.Identity.Application.TwoFactor;
using RowingClub.Identity.Application.TwoFactor.GenerateRecoveryCodes;

namespace RowingClub.Api.Endpoints;

public sealed record RegisterRequest(string Email, string Password);
public sealed record RegisterCompanyRequest(
    string CompanyName, string AdminEmail, string AdminPassword, string? Phone, string? ContactEmail, string? Address);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record VerifyTwoFactorRequest(string PendingToken, string Code);
public sealed record LogoutRequest(string RefreshToken);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
public sealed record VerifyEmailRequest(string Email, string Token);
public sealed record EnableTwoFactorRequest(string Method, string? Code, string? Secret);
public sealed record DisableTwoFactorRequest(string Password);

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
            .Produces<ApiResponse<RegisterResponse>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/companies/register", RegisterCompanyAsync)
            .WithName("RegisterCompany")
            .Produces<ApiResponse<RegisterCompanyResponse>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .Produces<ApiResponse<LoginResult>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/login/verify-2fa", VerifyTwoFactorLoginAsync)
            .WithName("VerifyTwoFactorLogin")
            .Produces<ApiResponse<VerifyTwoFactorLoginResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshAccessToken")
            .Produces<ApiResponse<RefreshTokenResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapPost("/logout-all", LogoutAllAsync)
            .WithName("LogoutAllDevices")
            .RequireAuthorization()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .WithName("ForgotPassword")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapPost("/reset-password", ResetPasswordAsync)
            .WithName("ResetPassword")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/verify-email", VerifyEmailAsync)
            .WithName("VerifyEmail")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/send-verification-email", SendVerificationEmailAsync)
            .WithName("SendVerificationEmail")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        // 2FA endpoints - require authentication
        var twoFactorGroup = group.MapGroup("")
            .RequireAuthorization();

        twoFactorGroup.MapGet("/2fa/setup", SetupTotpAsync)
            .WithName("SetupTotp")
            .Produces<ApiResponse<SetupTotpResponse>>(StatusCodes.Status200OK);

        twoFactorGroup.MapPost("/2fa/enable", EnableTwoFactorAsync)
            .WithName("EnableTwoFactor")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict);

        twoFactorGroup.MapPost("/2fa/disable", DisableTwoFactorAsync)
            .WithName("DisableTwoFactor")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict);

        twoFactorGroup.MapPost("/2fa/recovery-codes", GenerateRecoveryCodesAsync)
            .WithName("GenerateRecoveryCodes")
            .Produces<ApiResponse<GenerateRecoveryCodesResponse>>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new RegisterCommand(request.Email, request.Password), cancellationToken);
        return Results.Created($"/api/v1/me", ApiResponse<RegisterResponse>.Ok(response));
    }

    private static async Task<IResult> RegisterCompanyAsync(
        [FromBody] RegisterCompanyRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new RegisterCompanyCommand(
            request.CompanyName, request.AdminEmail, request.AdminPassword,
            request.Phone, request.ContactEmail, request.Address), cancellationToken);
        return Results.Created($"/api/v1/companies/{response.CompanyId}", ApiResponse<RegisterCompanyResponse>.Ok(response));
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
    {
        var deviceInfo = httpContext.Request.Headers.UserAgent.ToString();
        var result = await sender.Send(
            new LoginCommand(request.Email, request.Password, deviceInfo), cancellationToken);
        return Results.Ok(ApiResponse<LoginResult>.Ok(result));
    }

    private static async Task<IResult> VerifyTwoFactorLoginAsync(
        [FromBody] VerifyTwoFactorRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new VerifyTwoFactorLoginCommand(request.PendingToken, request.Code), cancellationToken);
        return Results.Ok(ApiResponse<VerifyTwoFactorLoginResponse>.Ok(response));
    }

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);
        return Results.Ok(ApiResponse<RefreshTokenResponse>.Ok(response));
    }

    private static async Task<IResult> LogoutAsync(
        [FromBody] LogoutRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return Results.Ok(ApiResponse.Ok("Oturum kapatıldı."));
    }

    private static async Task<IResult> LogoutAllAsync(ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutAllCommand(), cancellationToken);
        return Results.Ok(ApiResponse.Ok("Tüm oturumlar kapatıldı."));
    }

    private static async Task<IResult> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
        return Results.Ok(ApiResponse.Ok("Parola sıfırlama bağlantısı e-posta adresinize gönderildi."));
    }

    private static async Task<IResult> ResetPasswordAsync(
        [FromBody] ResetPasswordRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new ResetPasswordCommand(request.Email, request.Token, request.NewPassword), cancellationToken);
        return Results.Ok(ApiResponse.Ok("Parolanız başarıyla sıfırlandı."));
    }

    private static async Task<IResult> VerifyEmailAsync(
        [FromBody] VerifyEmailRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new VerifyEmailCommand(request.Email, request.Token), cancellationToken);
        return Results.Ok(ApiResponse.Ok("E-posta adresiniz başarıyla doğrulandı."));
    }

    private static async Task<IResult> SendVerificationEmailAsync(
        [FromBody] ForgotPasswordRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new SendVerificationEmailCommand(request.Email), cancellationToken);
        return Results.Ok(ApiResponse.Ok("Doğrulama e-postası gönderildi."));
    }

    private static async Task<IResult> SetupTotpAsync(
        ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new SetupTotpQuery(), cancellationToken);
        return Results.Ok(ApiResponse<SetupTotpResponse>.Ok(response));
    }

    private static async Task<IResult> EnableTwoFactorAsync(
        [FromBody] EnableTwoFactorRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new EnableTwoFactorCommand(request.Method, request.Code, request.Secret), cancellationToken);
        return Results.Ok(ApiResponse.Ok("İki faktörlü doğrulama etkinleştirildi."));
    }

    private static async Task<IResult> DisableTwoFactorAsync(
        [FromBody] DisableTwoFactorRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DisableTwoFactorCommand(request.Password), cancellationToken);
        return Results.Ok(ApiResponse.Ok("İki faktörlü doğrulama devre dışı bırakıldı."));
    }

    private static async Task<IResult> GenerateRecoveryCodesAsync(
        ICurrentUser user, ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GenerateRecoveryCodesCommand(user.UserId), cancellationToken);
        return Results.Ok(ApiResponse<GenerateRecoveryCodesResponse>.Ok(response));
    }
}
