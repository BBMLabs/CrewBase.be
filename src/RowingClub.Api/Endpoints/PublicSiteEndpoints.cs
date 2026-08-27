using MediatR;
using Microsoft.AspNetCore.Mvc;
using RowingClub.Api.Tenancy;
using RowingClub.Scheduling.Application.Availability;
using RowingClub.Scheduling.Application.Booking;
using RowingClub.Scheduling.Application.Members;
using RowingClub.Scheduling.Application.PublicOptions;

namespace RowingClub.Api.Endpoints;

public sealed record PublicBookingRequest(
    string FullName, string Phone, string? Email, string Date, string Time,
    string? BoatClass, string? Note, int? ReminderMinutes, List<string>? AcceptedConsents);

/// <summary>
/// Her firmanın müşterilere açık randevu akışı. Gerçekte {subdomain}.faturebase.com'dan servis
/// edilir; domain şimdilik mock olduğundan subdomain, path parametresi olarak da alınabilir.
/// </summary>
public static class PublicSiteEndpoints
{
    public static IEndpointRouteBuilder MapPublicSiteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Ok(ApiResponse.Ok("CrewBase API.")))
            .ExcludeFromDescription();

        var group = app.MapGroup("/api/v1/public/{subdomain}")
            .WithTags("Public Site");

        group.MapGet("/info", async (
            string subdomain, TenantResolver resolver, CancellationToken cancellationToken) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, cancellationToken);
            return company is null
                ? Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."))
                : Results.Ok(ApiResponse<object>.Ok(new
                {
                    company.Name,
                    company.Subdomain,
                    SiteUrl = $"https://{company.Subdomain}.{TenantResolver.BaseDomain}",
                    company.Phone,
                    company.ContactEmail,
                    company.Address,
                }));
        }).WithName("PublicCompanyInfo");

        // Kök site (/): birden fazla şube varsa seçim listesi için aktif şubeler.
        group.MapGet("/branches", async (
            string subdomain, TenantResolver resolver, ISender sender, CancellationToken cancellationToken) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, cancellationToken);
            if (company is null)
                return Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));

            var branches = await sender.Send(new GetPublicBranchListQuery(), cancellationToken);
            return Results.Ok(ApiResponse<List<PublicBranchSummaryDto>>.Ok(branches));
        }).WithName("PublicBranchList");

        // Şubenin kendi tekil sitesi (/sube/{code}): şube adı/adres/telefon burada, randevu
        // akışının geri kalanı (options/availability/appointments) firma genelinde kalır.
        group.MapGet("/branches/{code}", async (
            string subdomain, string code, TenantResolver resolver, ISender sender, CancellationToken cancellationToken) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, cancellationToken);
            if (company is null)
                return Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));

            var branch = await sender.Send(new GetPublicBranchQuery(code), cancellationToken);
            return branch is null
                ? Results.NotFound(ApiResponse.Fail("branch_not_found", "Şube bulunamadı."))
                : Results.Ok(ApiResponse<PublicBranchDto>.Ok(branch));
        }).WithName("PublicBranch");

        // Firmanın dinamik kuralları: çalışma saatleri, tekne sınıfları, paketler, hatırlatma
        // seçenekleri - site bunlarla çizilir.
        group.MapGet("/options", async (
            string subdomain, TenantResolver resolver, ISender sender, CancellationToken cancellationToken) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, cancellationToken);
            if (company is null)
                return Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));

            var options = await sender.Send(new GetPublicOptionsQuery(), cancellationToken);
            return Results.Ok(ApiResponse<PublicOptionsDto>.Ok(options));
        }).WithName("PublicOptions");

        // Beyan kataloğu: misafir formu bunları HER randevuda, üye kaydı üyelik beyanlarını
        // BİR KEZ gösterir.
        group.MapGet("/consents", async (
            string subdomain, TenantResolver resolver, ISender sender, CancellationToken cancellationToken) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, cancellationToken);
            if (company is null)
                return Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));

            var consents = await sender.Send(new GetConsentStatusQuery(null), cancellationToken);
            return Results.Ok(ApiResponse<List<ConsentStateDto>>.Ok(consents));
        }).WithName("PublicConsents");

        group.MapGet("/availability", async (
            string subdomain, [FromQuery] string? date, [FromQuery] string? boatClass,
            [FromQuery] string? phone, TenantResolver resolver, ISender sender,
            CancellationToken cancellationToken) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, cancellationToken);
            if (company is null)
                return Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));

            if (!TryParseDate(date, out var parsedDate))
                return Results.BadRequest(ApiResponse.Fail("invalid_date", "Tarih YYYY-AA-GG biçiminde olmalıdır."));

            var slots = await sender.Send(
                new GetAvailabilityQuery(parsedDate, boatClass ?? "1x", phone), cancellationToken);
            return Results.Ok(ApiResponse<List<SlotDto>>.Ok(slots));
        }).WithName("PublicAvailability");

        group.MapPost("/appointments", async (
            string subdomain, [FromBody] PublicBookingRequest request, HttpContext httpContext,
            TenantResolver resolver, ISender sender, CancellationToken cancellationToken) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, cancellationToken);
            if (company is null)
                return Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));

            if (!TryParseDate(request.Date, out var date))
                return Results.BadRequest(ApiResponse.Fail("invalid_date", "Tarih YYYY-AA-GG biçiminde olmalıdır."));

            if (!TimeOnly.TryParseExact(request.Time, "HH:mm", out var time))
                return Results.BadRequest(ApiResponse.Fail("invalid_time", "Saat SS:dd biçiminde olmalıdır."));

            // Anonim rezervasyonda paket düşümü bilinçli olarak kapalıdır: yalnızca telefon
            // bilgisiyle başkasının paketi eritilemesin. Paket kullanımı üye girişi gerektirir.
            var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var clientIp = !string.IsNullOrWhiteSpace(forwarded)
                ? forwarded.Split(',')[0].Trim()
                : httpContext.Connection.RemoteIpAddress?.ToString();

            var response = await sender.Send(new BookAppointmentCommand(
                request.FullName, request.Phone, request.Email, date, time,
                request.BoatClass ?? "1x", request.Note, request.ReminderMinutes,
                UsePackage: false, request.AcceptedConsents ?? [], clientIp), cancellationToken);

            return Results.Created(
                $"/api/v1/public/{subdomain}/appointments/{response.AppointmentId}",
                ApiResponse<BookAppointmentResponse>.Ok(response, "Randevunuz alındı."));
        }).WithName("PublicBookAppointment");

        return app;
    }

    private static bool TryParseDate(string? raw, out DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            date = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            return true;
        }

        return DateOnly.TryParseExact(raw, "yyyy-MM-dd", out date);
    }
}
