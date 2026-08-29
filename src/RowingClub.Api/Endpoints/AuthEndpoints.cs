using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RowingClub.Api.RateLimiting;
using RowingClub.Api.Tenancy;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Identity.Application.Companies.RegisterCompany;
using RowingClub.Identity.Application.EmailVerification;
using RowingClub.Identity.Application.Login;
using RowingClub.Identity.Application.Logout;
using RowingClub.Identity.Application.LogoutAll;
using RowingClub.Identity.Application.PasswordReset;
using RowingClub.Identity.Application.Refresh;
using RowingClub.Identity.Application.Register;
using RowingClub.Scheduling.Application.Panel;

namespace RowingClub.Api.Endpoints;

public sealed record RegisterCompanyRequest(
    string CompanyName, string AdminEmail, string TaxNumber, string Phone, string ContactEmail, string Address);
public sealed record LoginRequest(string Email, string Password, string? RecaptchaToken);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
public sealed record VerifyEmailRequest(string Email, string Token);

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

        // 2FA endpoint'leri bilinçli olarak pasife alındı (istek üzerine). Application katmanındaki
        // handler'lar duruyor; tekrar açmak için buradaki map'ler geri eklenmelidir.

        return app;
    }

    private static async Task<IResult> RegisterCompanyAsync(
        [FromBody] RegisterCompanyRequest request, ISender sender, TenantResolver resolver,
        ILogger<Program> logger, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new RegisterCompanyCommand(
            request.CompanyName, request.AdminEmail, request.TaxNumber,
            request.Phone, request.ContactEmail, request.Address), cancellationToken);

        if (await resolver.ResolveByCompanyIdAsync(response.CompanyId, cancellationToken) is not null)
        {
            try
            {
                await sender.Send(new CreateBranchCommand(
                    $"{response.CompanyName} Şube 1", null, null, null, null, null, null, null), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Firma kaydında ilk şube otomatik oluşturulamadı: {CompanyId}", response.CompanyId);
            }
        }

        return Results.Created($"/api/v1/companies/{response.CompanyId}", ApiResponse<RegisterCompanyResponse>.Ok(response));
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
    {
        var deviceInfo = httpContext.Request.Headers.UserAgent.ToString();
        var result = await sender.Send(
            new LoginCommand(request.Email, request.Password, deviceInfo, request.RecaptchaToken), cancellationToken);
        return Results.Ok(ApiResponse<LoginResult>.Ok(result));
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
}
