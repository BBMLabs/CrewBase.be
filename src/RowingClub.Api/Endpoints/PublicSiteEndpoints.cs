using MediatR;
using Microsoft.AspNetCore.Mvc;
using RowingClub.Api.Tenancy;
using RowingClub.Identity.Application.Companies.SiteContent;
using RowingClub.Scheduling.Application.Availability;
using RowingClub.Scheduling.Application.Booking;
using RowingClub.Scheduling.Application.Community;
using RowingClub.Scheduling.Application.Members;
using RowingClub.Scheduling.Application.Messages;
using RowingClub.Scheduling.Application.PublicOptions;
using RowingClub.Scheduling.Application.Rsvp;

namespace RowingClub.Api.Endpoints;

public sealed record PublicBookingRequest(
    string FullName, string Phone, string? Email, string Date, string Time,
    string? BoatClass, bool ExperienceAcknowledged, string? TeammateName, string? Note, int? ReminderMinutes,
    List<string>? AcceptedConsents);

public sealed record PublicRsvpRequest(string? Choice);

public sealed record PublicSiteMessageRequest(string FullName, string Email, string Body, string? Phone = null);

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
                    company.LogoPath,
                    company.Tagline,
                    company.AboutText,
                    company.InstagramUrl,
                    company.FacebookUrl,
                    company.YoutubeUrl,
                    company.LinkedinUrl,
                    company.XUrl,
                    company.WhatsappUrl,
                    company.TelegramUrl,
                    company.PinterestUrl,
                    company.GoogleMapsUrl,
                    company.Phone,
                    company.ContactEmail,
                    company.Address,
                }));
        }).WithName("PublicCompanyInfo");

        group.MapGet("/gallery", async (
            string subdomain, TenantResolver resolver, ISender sender, CancellationToken cancellationToken) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, cancellationToken);
            if (company is null)
                return Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));

            var content = await sender.Send(new GetCompanySiteContentQuery(company.CompanyId), cancellationToken);
            return Results.Ok(ApiResponse<List<string>>.Ok(content.GalleryImages.Select(i => i.ImagePath).ToList()));
        }).WithName("PublicCompanyGallery");

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

        // Şubenin kendi tekil sitesi (/branch/{code}): şube adı/adres/telefon burada, randevu
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
                request.BoatClass ?? "1x", request.ExperienceAcknowledged, request.TeammateName, request.Note,
                request.ReminderMinutes, UsePackage: false, request.AcceptedConsents ?? [], clientIp,
                company.Name, company.Subdomain), cancellationToken);

            return Results.Created(
                $"/api/v1/public/{subdomain}/appointments/{response.AppointmentId}",
                ApiResponse<BookAppointmentResponse>.Ok(response, "Randevunuz alındı."));
        }).WithName("PublicBookAppointment");

        group.MapGet("/rsvp/{token}", async (
            string subdomain, string token, TenantResolver resolver, ISender sender, CancellationToken cancellationToken) =>
        {
            if (!IsPlausibleRsvpToken(token))
                return RsvpNotFound();

            var company = await resolver.ResolveBySubdomainAsync(subdomain, cancellationToken);
            if (company is null)
                return Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));

            var rsvp = await sender.Send(new GetRsvpQuery(token, company.Name), cancellationToken);
            return rsvp is null ? RsvpNotFound() : Results.Ok(ApiResponse<RsvpDto>.Ok(rsvp));
        }).WithName("PublicGetRsvp");

        group.MapPost("/rsvp/{token}", async (
            string subdomain, string token, [FromBody] PublicRsvpRequest request,
            TenantResolver resolver, ISender sender, CancellationToken cancellationToken) =>
        {
            if (!RsvpMapper.IsValidChoice(request.Choice))
                return RsvpInvalidChoice();

            if (!IsPlausibleRsvpToken(token))
                return RsvpNotFound();

            var company = await resolver.ResolveBySubdomainAsync(subdomain, cancellationToken);
            if (company is null)
                return Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));

            var result = await sender.Send(new SubmitRsvpCommand(token, request.Choice!, company.Name), cancellationToken);
            return result.Outcome switch
            {
                RsvpSubmitOutcome.Saved => Results.Ok(ApiResponse<RsvpDto>.Ok(result.Rsvp!, "Yanıtınız kaydedildi.")),
                RsvpSubmitOutcome.Closed => Results.BadRequest(ApiResponse.Fail("rsvp_closed", "Yanıt süresi doldu.")),
                RsvpSubmitOutcome.InvalidChoice => RsvpInvalidChoice(),
                _ => RsvpNotFound(),
            };
        }).WithName("PublicSubmitRsvp");

        group.MapGet("/feed", async (
            string subdomain, TenantResolver resolver, ISender sender, CancellationToken cancellationToken) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, cancellationToken);
            if (company is null)
                return Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));

            var posts = await sender.Send(new GetFeedQuery(null, 50, ClubOnly: true), cancellationToken);
            return Results.Ok(ApiResponse<List<PostDto>>.Ok(posts));
        }).WithName("PublicFeed");

        group.MapGet("/feed/{postId:guid}/media", async (
            string subdomain, Guid postId, TenantResolver resolver, ISender sender, CancellationToken cancellationToken) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, cancellationToken);
            if (company is null)
                return Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));

            var media = await sender.Send(new GetPostMediaQuery(postId, ClubOnly: true), cancellationToken);
            return media is null
                ? Results.NotFound(ApiResponse.Fail("no_media", "Bu paylaşımda medya yok."))
                : Results.Ok(ApiResponse<PostMediaDto>.Ok(media));
        }).WithName("PublicFeedPostMedia");

        group.MapGet("/feed/{postId:guid}/comments", async (
            string subdomain, Guid postId, TenantResolver resolver, ISender sender, CancellationToken cancellationToken) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, cancellationToken);
            if (company is null)
                return Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));

            var comments = await sender.Send(new GetCommentsQuery(postId, ClubOnly: true), cancellationToken);
            return Results.Ok(ApiResponse<List<CommentDto>>.Ok(comments));
        }).WithName("PublicFeedPostComments");

        group.MapGet("/feed/{postId:guid}/participants", async (
            string subdomain, Guid postId, TenantResolver resolver, ISender sender, CancellationToken cancellationToken) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, cancellationToken);
            if (company is null)
                return Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));

            var participants = await sender.Send(new GetParticipantsQuery(postId, ClubOnly: true), cancellationToken);
            return Results.Ok(ApiResponse<List<ParticipantDto>>.Ok(participants));
        }).WithName("PublicFeedPostParticipants");

        group.MapPost("/messages", async (
            string subdomain, [FromBody] PublicSiteMessageRequest request, HttpContext httpContext,
            TenantResolver resolver, ISender sender, CancellationToken cancellationToken) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, cancellationToken);
            if (company is null)
                return Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));

            var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var clientIp = !string.IsNullOrWhiteSpace(forwarded)
                ? forwarded.Split(',')[0].Trim()
                : httpContext.Connection.RemoteIpAddress?.ToString();

            await sender.Send(
                new SubmitSiteMessageCommand(request.FullName, request.Email, request.Phone ?? string.Empty, request.Body, clientIp),
                cancellationToken);

            return Results.Ok(ApiResponse.Ok("Mesajınız iletildi."));
        }).WithName("PublicSubmitSiteMessage");

        return app;
    }

    private static bool IsPlausibleRsvpToken(string? token) =>
        !string.IsNullOrWhiteSpace(token) && token.Length <= 256;

    private static IResult RsvpNotFound() =>
        Results.NotFound(ApiResponse.Fail("rsvp_not_found", "Katılım onayı bağlantısı bulunamadı."));

    private static IResult RsvpInvalidChoice() =>
        Results.BadRequest(ApiResponse.Fail(
            "rsvp_invalid_choice", "Geçersiz yanıt. \"Katılıyorum\" veya \"Katılamıyorum\" seçilmelidir."));

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
