using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowingClub.Api.RateLimiting;
using RowingClub.Api.Tenancy;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Identity.Application.Companies.GetCompanySite;
using RowingClub.Scheduling.Application.Booking;
using RowingClub.Scheduling.Application.Community;
using RowingClub.Scheduling.Application.Members;

namespace RowingClub.Api.Endpoints;

public sealed record MemberRegisterRequest(
    string FullName, string Phone, string Email, List<string>? AcceptedConsents,
    string? BranchCode);
public sealed record MemberLoginRequest(string Email, string Password);
public sealed record MemberSetPasswordRequest(string Email, string Token, string NewPassword);
public sealed record MemberForgotPasswordRequest(string Email);
public sealed record MemberUpdateRequest(string FullName, string? Email, int? DefaultReminderMinutes);
public sealed record MemberDeleteRequest(string Password);
public sealed record MemberBookingRequest(
    string Date, string Time, string? BoatClass, string? TeammateName, string? Note, int? ReminderMinutes,
    bool UsePackage, List<string>? AcceptedConsents);
public sealed record FriendAddRequest(string MemberCode);
public sealed record MessageSendRequest(string Body);
public sealed record ConsentSubmitRequest(List<ConsentEntry> Entries);
public sealed record CardUpsertRequest(
    string Type, string? CardNumber, string CompanyName, string? ExpiryDate, string Status,
    string? PhotoBase64, string? PhotoContentType);
public sealed record OtpRequest(string Purpose);
public sealed record OtpVerifyRequest(string Purpose, string Code);
public sealed record PostCreateRequest(
    string Body, string? MediaBase64, string? MediaContentType,
    bool IsEvent, string? EventTitle, string? EventDate);
public sealed record CommentCreateRequest(string Body);
public sealed record ConfirmPackagePurchaseRequest(Guid PackageId, string Token);

/// <summary>
/// Üye tarafı: subdomain üzerinden kayıt/giriş (anonim) ve Member rolüyle korunan self-servis
/// uçlar: profil, randevu, paket, beyanlar, kartlar, OTP doğrulama, arkadaş/mesaj ve kulüp akışı.
/// Kimlik JWT'den (sub=CustomerId, company_id) gelir; istemciden id kabul edilmez.
/// </summary>
public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        // ---- Anonim: üye kayıt/giriş (subdomain'e, yani firmaya bağlı) ----
        var publicGroup = app.MapGroup("/api/v1/public/{subdomain}/members")
            .WithTags("Member Auth")
            .RequireRateLimiting(RateLimitingSetup.AuthPolicy);

        publicGroup.MapPost("/register", async (
            string subdomain, [FromBody] MemberRegisterRequest request, HttpContext http,
            TenantResolver resolver, MemberTokenIssuer tokenIssuer, ISender sender, CancellationToken ct) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, ct);
            if (company is null)
                return CompanyNotFound();

            var member = await sender.Send(new RegisterMemberCommand(
                request.FullName, request.Phone, request.Email,
                request.AcceptedConsents ?? [], ClientIp(http), request.BranchCode,
                company.Name, company.Subdomain), ct);

            var (token, expiresAt) = tokenIssuer.Issue(member, company);
            return Results.Ok(ApiResponse<object>.Ok(
                new { member, accessToken = token, expiresAtUtc = expiresAt }, "Üyeliğiniz oluşturuldu."));
        }).WithName("MemberRegister");

        publicGroup.MapPost("/login", async (
            string subdomain, [FromBody] MemberLoginRequest request, TenantResolver resolver,
            MemberTokenIssuer tokenIssuer, ISender sender, CancellationToken ct) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, ct);
            if (company is null)
                return CompanyNotFound();

            var member = await sender.Send(new MemberLoginCommand(request.Email, request.Password), ct);

            var (token, expiresAt) = tokenIssuer.Issue(member, company);
            return Results.Ok(ApiResponse<object>.Ok(
                new { member, accessToken = token, expiresAtUtc = expiresAt }));
        }).WithName("MemberLogin");

        publicGroup.MapPost("/set-password", async (
            string subdomain, [FromBody] MemberSetPasswordRequest request, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, ct);
            if (company is null)
                return CompanyNotFound();

            await sender.Send(new SetMemberPasswordCommand(request.Email, request.Token, request.NewPassword), ct);
            return Results.Ok(ApiResponse<object?>.Ok(null, "Şifreniz oluşturuldu."));
        }).WithName("MemberSetPassword");

        publicGroup.MapPost("/forgot-password", async (
            string subdomain, [FromBody] MemberForgotPasswordRequest request, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            var company = await resolver.ResolveBySubdomainAsync(subdomain, ct);
            if (company is null)
                return CompanyNotFound();

            await sender.Send(new RequestMemberPasswordResetCommand(request.Email, company.Name, company.Subdomain), ct);
            return Results.Ok(ApiResponse<object?>.Ok(null, "E-posta adresiniz kayıtlıysa bir sıfırlama bağlantısı gönderildi."));
        }).WithName("MemberForgotPassword");

        // ---- Member rolü gerektiren self-servis uçlar ----
        var group = app.MapGroup("/api/v1/member")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Member" })
            .WithTags("Member Panel");

        // -- profil --

        group.MapGet("/me", Guarded(async (ctx, sender, ct) =>
        {
            var profile = await sender.Send(new GetMemberProfileQuery(ctx.CustomerId), ct);
            return Results.Ok(ApiResponse<MemberDto>.Ok(profile));
        })).WithName("MemberProfile");

        group.MapPut("/me", GuardedBody<MemberUpdateRequest>(async (ctx, request, sender, ct) =>
        {
            var profile = await sender.Send(new UpdateMemberProfileCommand(
                ctx.CustomerId, request.FullName, request.Email, request.DefaultReminderMinutes), ct);
            return Results.Ok(ApiResponse<MemberDto>.Ok(profile, "Bilgileriniz güncellendi."));
        })).WithName("MemberUpdateProfile");

        group.MapDelete("/me", GuardedBody<MemberDeleteRequest>(async (ctx, request, sender, ct) =>
        {
            await sender.Send(new DeleteMemberAccountCommand(ctx.CustomerId, request.Password), ct);
            return Results.Ok(ApiResponse.Ok("Hesabınız ve tüm verileriniz kalıcı olarak silindi."));
        })).WithName("MemberDeleteAccount");

        group.MapPost("/heartbeat", Guarded(async (ctx, sender, ct) =>
        {
            await sender.Send(new RecordMemberActivityCommand(ctx.CustomerId), ct);
            return Results.Ok(ApiResponse.Ok());
        })).WithName("MemberHeartbeat");

        // -- OTP doğrulama (e-posta / telefon) --

        group.MapPost("/otp/request", GuardedBody<OtpRequest>(async (ctx, request, sender, ct) =>
        {
            await sender.Send(new RequestMemberOtpCommand(ctx.CustomerId, request.Purpose), ct);
            return Results.Ok(ApiResponse.Ok("Doğrulama kodu gönderildi."));
        })).WithName("MemberOtpRequest");

        group.MapPost("/otp/verify", GuardedBody<OtpVerifyRequest>(async (ctx, request, sender, ct) =>
        {
            var profile = await sender.Send(new VerifyMemberOtpCommand(ctx.CustomerId, request.Purpose, request.Code), ct);
            return Results.Ok(ApiResponse<MemberDto>.Ok(profile, "Doğrulama tamamlandı."));
        })).WithName("MemberOtpVerify");

        // -- randevular --

        group.MapGet("/appointments", Guarded(async (ctx, sender, ct) =>
        {
            var appointments = await sender.Send(new GetMemberAppointmentsQuery(ctx.CustomerId), ct);
            return Results.Ok(ApiResponse<List<MemberAppointmentDto>>.Ok(appointments));
        })).WithName("MemberAppointments");

        group.MapPost("/appointments", GuardedBody<MemberBookingRequest>(async (ctx, request, sender, ct) =>
        {
            if (!DateOnly.TryParseExact(request.Date, "yyyy-MM-dd", out var date))
                return Results.BadRequest(ApiResponse.Fail("invalid_date", "Tarih YYYY-AA-GG biçiminde olmalıdır."));

            if (!TimeOnly.TryParseExact(request.Time, "HH:mm", out var time))
                return Results.BadRequest(ApiResponse.Fail("invalid_time", "Saat SS:dd biçiminde olmalıdır."));

            // Üye kendi profil bilgileriyle rezervasyon yapar - istemciden ad/telefon alınmaz.
            var profile = await sender.Send(new GetMemberProfileQuery(ctx.CustomerId), ct);

            var response = await sender.Send(new BookAppointmentCommand(
                profile.FullName, profile.Phone, profile.Email, date, time,
                request.BoatClass ?? "1x", ExperienceAcknowledged: true, request.TeammateName, request.Note,
                request.ReminderMinutes, request.UsePackage, request.AcceptedConsents ?? [], ctx.Ip), ct);

            return Results.Created(
                $"/api/v1/member/appointments/{response.AppointmentId}",
                ApiResponse<BookAppointmentResponse>.Ok(response, "Randevunuz alındı."));
        })).WithName("MemberBookAppointment");

        group.MapPost("/appointments/{id:guid}/cancel", GuardedRoute(async (ctx, appointmentId, sender, ct) =>
        {
            await sender.Send(new CancelAppointmentCommand(appointmentId, ctx.CustomerId), ct);
            return Results.Ok(ApiResponse.Ok("Randevunuz iptal edildi."));
        })).WithName("MemberCancelAppointment");

        group.MapGet("/packages", Guarded(async (ctx, sender, ct) =>
        {
            var packages = await sender.Send(new GetMemberPackagesQuery(ctx.CustomerId), ct);
            return Results.Ok(ApiResponse<List<CustomerPackageDto>>.Ok(packages));
        })).WithName("MemberPackages");

        group.MapGet("/packages/catalog", Guarded(async (ctx, sender, ct) =>
        {
            var catalog = await sender.Send(new GetPurchasablePackagesQuery(ctx.CustomerId), ct);
            return Results.Ok(ApiResponse<List<PurchasablePackageDto>>.Ok(catalog));
        })).WithName("MemberPackageCatalog");

        // GuardedRoute yol parametresini "id" adıyla bağlar; "{packageId}" yazıldığında Guid sorgu dizisinde
        // aranıyor ve uç her istekte 400/500 dönüyordu.
        group.MapPost("/packages/{id:guid}/purchase", GuardedRoute(async (ctx, packageId, sender, ct) =>
        {
            // Ham SPA route'u DEĞİL - iyzico callback'i token'ı form POST ile gönderir, SPA bunu
            // yakalayamaz; bkz. WebhookEndpoints.MapWebhookEndpoints'teki POST->GET köprüsü. "tenant"
            // burada taşınır çünkü köprü, hangi firma alt alanına geri yönlendireceğini bilemez.
            var callbackUrl =
                $"https://{TenantResolver.BaseDomain}/api/v1/webhooks/iyzico/checkout-callback/package?tenant={ctx.Subdomain}";
            var result = await sender.Send(
                new PurchasePackageCommand(ctx.CustomerId, packageId, callbackUrl, ctx.Ip ?? "0.0.0.0"), ct);
            return Results.Ok(ApiResponse<PurchasePackageResult>.Ok(result));
        })).WithName("MemberPurchasePackage");

        group.MapPost("/packages/purchase/checkout-result", GuardedBody<ConfirmPackagePurchaseRequest>(
            async (ctx, request, sender, ct) =>
        {
            var package = await sender.Send(
                new ConfirmPackagePurchaseCommand(ctx.CustomerId, request.PackageId, request.Token), ct);
            return Results.Ok(ApiResponse<CustomerPackageDto>.Ok(package, "Paketiniz satın alındı."));
        })).WithName("MemberConfirmPackagePurchase");

        // -- beyanlar --

        group.MapGet("/consents", Guarded(async (ctx, sender, ct) =>
        {
            var consents = await sender.Send(new GetConsentStatusQuery(ctx.CustomerId), ct);
            return Results.Ok(ApiResponse<List<ConsentStateDto>>.Ok(consents));
        })).WithName("MemberConsents");

        group.MapPost("/consents", GuardedBody<ConsentSubmitRequest>(async (ctx, request, sender, ct) =>
        {
            var consents = await sender.Send(new SubmitConsentsCommand(ctx.CustomerId, request.Entries, ctx.Ip), ct);
            return Results.Ok(ApiResponse<List<ConsentStateDto>>.Ok(consents, "Beyanlarınız kaydedildi."));
        })).WithName("MemberSubmitConsents");

        // -- üyelik kartları (Multisport / Meditopia) --

        group.MapGet("/cards", Guarded(async (ctx, sender, ct) =>
        {
            var cards = await sender.Send(new GetMyCardsQuery(ctx.CustomerId, IncludePhotos: true), ct);
            return Results.Ok(ApiResponse<List<CardDto>>.Ok(cards));
        })).WithName("MemberCards");

        group.MapPut("/cards", GuardedBody<CardUpsertRequest>(async (ctx, request, sender, ct) =>
        {
            var card = await sender.Send(new UpsertCardCommand(
                ctx.CustomerId, request.Type, request.CardNumber, request.CompanyName,
                request.ExpiryDate, request.Status, request.PhotoBase64, request.PhotoContentType), ct);
            return Results.Ok(ApiResponse<CardDto>.Ok(card, "Kart bilgileriniz kaydedildi."));
        })).WithName("MemberUpsertCard");

        // -- arkadaşlar & mesajlar --

        group.MapGet("/code", Guarded(async (ctx, sender, ct) =>
        {
            var code = await sender.Send(new GetMyCodeQuery(ctx.CustomerId), ct);
            return Results.Ok(ApiResponse<object>.Ok(new { memberCode = code }));
        })).WithName("MemberCode");

        group.MapGet("/friends", Guarded(async (ctx, sender, ct) =>
        {
            var friends = await sender.Send(new GetFriendsQuery(ctx.CustomerId), ct);
            return Results.Ok(ApiResponse<List<FriendDto>>.Ok(friends));
        })).WithName("MemberFriends");

        group.MapPost("/friends", GuardedBody<FriendAddRequest>(async (ctx, request, sender, ct) =>
        {
            var friend = await sender.Send(new SendFriendRequestCommand(ctx.CustomerId, request.MemberCode), ct);
            return Results.Ok(ApiResponse<FriendDto>.Ok(friend, "Arkadaşlık isteği gönderildi."));
        })).WithName("MemberAddFriend");

        group.MapPost("/friends/{id:guid}/accept", GuardedRoute(async (ctx, friendshipId, sender, ct) =>
        {
            await sender.Send(new RespondFriendRequestCommand(ctx.CustomerId, friendshipId, true), ct);
            return Results.Ok(ApiResponse.Ok("Arkadaşlık isteği kabul edildi."));
        })).WithName("MemberAcceptFriend");

        group.MapPost("/friends/{id:guid}/reject", GuardedRoute(async (ctx, friendshipId, sender, ct) =>
        {
            await sender.Send(new RespondFriendRequestCommand(ctx.CustomerId, friendshipId, false), ct);
            return Results.Ok(ApiResponse.Ok("Arkadaşlık isteği reddedildi."));
        })).WithName("MemberRejectFriend");

        group.MapGet("/messages/{id:guid}", GuardedRoute(async (ctx, friendCustomerId, sender, ct) =>
        {
            var messages = await sender.Send(new GetConversationQuery(ctx.CustomerId, friendCustomerId, 100), ct);
            return Results.Ok(ApiResponse<List<MessageDto>>.Ok(messages));
        })).WithName("MemberConversation");

        group.MapPost("/messages/{id:guid}", GuardedRouteBody<MessageSendRequest>(
            async (ctx, friendCustomerId, request, sender, ct) =>
        {
            var message = await sender.Send(new SendDirectMessageCommand(ctx.CustomerId, friendCustomerId, request.Body), ct);
            return Results.Ok(ApiResponse<MessageDto>.Ok(message));
        })).WithName("MemberSendMessage");

        // -- kulüp akışı --

        group.MapGet("/feed", Guarded(async (ctx, sender, ct) =>
        {
            var posts = await sender.Send(new GetFeedQuery(ctx.CustomerId, 50, ClubOnly: false), ct);
            return Results.Ok(ApiResponse<List<PostDto>>.Ok(posts));
        })).WithName("MemberFeed");

        group.MapPost("/feed", GuardedBody<PostCreateRequest>(async (ctx, request, sender, ct) =>
        {
            var postId = await sender.Send(new CreatePostCommand(
                ctx.CustomerId, request.Body, request.MediaBase64, request.MediaContentType,
                request.IsEvent, request.EventTitle, request.EventDate), ct);
            return Results.Ok(ApiResponse<object>.Ok(new { postId }, "Paylaşımınız yayınlandı."));
        })).WithName("MemberCreatePost");

        group.MapGet("/feed/{id:guid}/media", GuardedRoute(async (ctx, postId, sender, ct) =>
        {
            var media = await sender.Send(new GetPostMediaQuery(postId, ClubOnly: false), ct);
            return media is null
                ? Results.NotFound(ApiResponse.Fail("no_media", "Bu paylaşımda medya yok."))
                : Results.Ok(ApiResponse<PostMediaDto>.Ok(media));
        })).WithName("MemberPostMedia");

        group.MapPost("/feed/{id:guid}/delete", GuardedRoute(async (ctx, postId, sender, ct) =>
        {
            await sender.Send(new DeletePostCommand(postId, ctx.CustomerId, IsCompany: false), ct);
            return Results.Ok(ApiResponse.Ok("Paylaşım silindi."));
        })).WithName("MemberDeletePost");

        group.MapPost("/feed/{id:guid}/like", GuardedRoute(async (ctx, postId, sender, ct) =>
        {
            var result = await sender.Send(new ToggleLikeCommand(postId, ctx.CustomerId), ct);
            return Results.Ok(ApiResponse<ToggleResult>.Ok(result));
        })).WithName("MemberToggleLike");

        group.MapGet("/feed/{id:guid}/comments", GuardedRoute(async (ctx, postId, sender, ct) =>
        {
            var comments = await sender.Send(new GetCommentsQuery(postId, ClubOnly: false), ct);
            return Results.Ok(ApiResponse<List<CommentDto>>.Ok(comments));
        })).WithName("MemberComments");

        group.MapPost("/feed/{id:guid}/comments", GuardedRouteBody<CommentCreateRequest>(
            async (ctx, postId, request, sender, ct) =>
        {
            var comment = await sender.Send(new AddCommentCommand(postId, ctx.CustomerId, request.Body), ct);
            return Results.Ok(ApiResponse<CommentDto>.Ok(comment));
        })).WithName("MemberAddComment");

        group.MapPost("/feed/{id:guid}/join", GuardedRoute(async (ctx, postId, sender, ct) =>
        {
            var result = await sender.Send(new ToggleParticipationCommand(postId, ctx.CustomerId), ct);
            return Results.Ok(ApiResponse<ToggleResult>.Ok(result));
        })).WithName("MemberToggleJoin");

        group.MapGet("/feed/{id:guid}/participants", GuardedRoute(async (ctx, postId, sender, ct) =>
        {
            var participants = await sender.Send(new GetParticipantsQuery(postId, ClubOnly: false), ct);
            return Results.Ok(ApiResponse<List<ParticipantDto>>.Ok(participants));
        })).WithName("MemberParticipants");

        group.MapPost("/follow/{id:guid}", GuardedRoute(async (ctx, customerId, sender, ct) =>
        {
            var result = await sender.Send(new ToggleFollowCommand(ctx.CustomerId, customerId), ct);
            return Results.Ok(ApiResponse<ToggleResult>.Ok(result));
        })).WithName("MemberToggleFollow");

        return app;
    }

    // ---- Ortak sarmalayıcılar: tenant çözümü + üye bağlamı ----

    private sealed record MemberContext(Guid CustomerId, string? Ip, string Subdomain, string CompanyName);

    private static string? ClientIp(HttpContext http)
    {
        var forwarded = http.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();
        return http.Connection.RemoteIpAddress?.ToString();
    }

    private static async Task<(MemberContext? Ctx, IResult? Error)> ResolveAsync(
        HttpContext http, ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct)
    {
        CompanySiteDto? company = user.CompanyId is { } companyId
            ? await resolver.ResolveByCompanyIdAsync(companyId, ct)
            : null;

        if (company is null)
            return (null, CompanyNotFound());

        // Token geçerli olsa da üye silinmiş/engellenmişse oturum biter: gövdesiz 401, frontend'in
        // oturum-sonu akışını (girişe yönlendirme) tetikler; yeniden girişte engel mesajı gösterilir.
        if (!await sender.Send(new GetMemberAccessQuery(user.UserId), ct))
            return (null, Results.Unauthorized());

        return (new MemberContext(user.UserId, ClientIp(http), company.Subdomain, company.Name), null);
    }

    private static Delegate Guarded(Func<MemberContext, ISender, CancellationToken, Task<IResult>> handler) =>
        async (HttpContext http, ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            var (ctx, error) = await ResolveAsync(http, user, resolver, sender, ct);
            return error ?? await handler(ctx!, sender, ct);
        };

    private static Delegate GuardedBody<TBody>(
        Func<MemberContext, TBody, ISender, CancellationToken, Task<IResult>> handler) =>
        async (HttpContext http, [FromBody] TBody body, ICurrentUser user, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            var (ctx, error) = await ResolveAsync(http, user, resolver, sender, ct);
            return error ?? await handler(ctx!, body, sender, ct);
        };

    private static Delegate GuardedRoute(
        Func<MemberContext, Guid, ISender, CancellationToken, Task<IResult>> handler) =>
        async (HttpContext http, Guid id, ICurrentUser user, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            var (ctx, error) = await ResolveAsync(http, user, resolver, sender, ct);
            return error ?? await handler(ctx!, id, sender, ct);
        };

    private static Delegate GuardedRouteBody<TBody>(
        Func<MemberContext, Guid, TBody, ISender, CancellationToken, Task<IResult>> handler) =>
        async (HttpContext http, Guid id, [FromBody] TBody body, ICurrentUser user, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            var (ctx, error) = await ResolveAsync(http, user, resolver, sender, ct);
            return error ?? await handler(ctx!, id, body, sender, ct);
        };

    private static IResult CompanyNotFound() =>
        Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));
}
