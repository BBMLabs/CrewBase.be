using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowingClub.Api.Tenancy;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Identity.Application.Companies.GetCompanySite;
using RowingClub.Identity.Application.CompanyUsers;
using RowingClub.Scheduling.Application.Community;
using RowingClub.Scheduling.Application.Panel;

namespace RowingClub.Api.Endpoints;

public sealed record SetAppointmentStatusRequest(string Status);
public sealed record SetCustomerLevelRequest(int Level);
public sealed record InstructorRequest(string FullName, string? Phone, string? Email, bool? IsActive, Guid? BranchId);
public sealed record BoatRequest(string Name, string BoatClass, bool? IsActive, Guid? BranchId);
public sealed record BranchRequest(string Name, string? Address, string? Phone, bool? IsActive);
public sealed record PackageRequest(string Name, string? Description, int SessionCount, decimal Price, bool? IsActive);
public sealed record AssignSessionRequest(Guid? BoatId, Guid? InstructorId);
public sealed record UpdateSettingsRequest(
    string OpeningTime, string ClosingTime, int SlotMinutes, List<int> OpenDays,
    int MinNoticeHours, int MaxAdvanceDays, List<int> ReminderOptions,
    int DefaultReminderMinutes, string TimeZoneId);
public sealed record CreateCompanyUserRequest(string Email, string Password, string Role);
public sealed record ChangeUserRoleRequest(string Role);
public sealed record CreateMemberRequest(string FullName, string Phone, string? Email, int Level);
public sealed record AssignPackageRequest(Guid LessonPackageId);
public sealed record ClosedDateRequest(string Date, string? Reason);

/// <summary>
/// Firma yöneticisinin paneli: randevular, seanslar (tekne/hoca atamaları), üye dereceleri,
/// eğitmen/tekne/paket tanımları, çalışma-saati ve hatırlatma kuralları, firma kullanıcıları.
/// Tüm yetki denetimi backend'dedir: rol JWT'den doğrulanır, firma oturumdaki CompanyId'den
/// bulunur ve veriler yalnızca o firmanın KENDİ tenant veritabanından okunur; istemciden gelen
/// hiçbir companyId/rol değerine güvenilmez.
/// </summary>
public static class CompanyPanelEndpoints
{
    public static IEndpointRouteBuilder MapCompanyPanelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/company")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "CompanyAdmin" })
            .WithTags("Company Panel");

        group.MapGet("/site", async (
            ICurrentUser user, TenantResolver resolver, CancellationToken ct) =>
        {
            var company = await ResolveOwnCompanyAsync(user, resolver, ct);
            return company is null
                ? CompanyNotFound()
                : Results.Ok(ApiResponse<object>.Ok(new
                {
                    company.Name,
                    company.Subdomain,
                    SiteUrl = $"https://{company.Subdomain}.{TenantResolver.BaseDomain}",
                    MockPath = $"/site/{company.Subdomain}",
                }));
        }).WithName("CompanySiteInfo");

        // ---- Randevular ----

        group.MapGet("/appointments", async (
            [FromQuery] string? date, ICurrentUser user, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            DateOnly? filterDate = null;
            if (!string.IsNullOrWhiteSpace(date))
            {
                if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsed))
                    return InvalidDate();
                filterDate = parsed;
            }

            var appointments = await sender.Send(new GetAppointmentsQuery(filterDate), ct);
            return Results.Ok(ApiResponse<List<AppointmentDto>>.Ok(appointments));
        }).WithName("CompanyAppointments");

        group.MapPost("/appointments/{appointmentId:guid}/status", async (
            Guid appointmentId, [FromBody] SetAppointmentStatusRequest request, ICurrentUser user,
            TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            await sender.Send(new SetAppointmentStatusCommand(appointmentId, request.Status), ct);
            return Results.Ok(ApiResponse.Ok("Randevu durumu güncellendi."));
        }).WithName("CompanySetAppointmentStatus");

        // ---- Seanslar (tekne/hoca atamalı grup antrenmanları) ----

        group.MapGet("/sessions", async (
            [FromQuery] string? date, ICurrentUser user, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            if (!DateOnly.TryParseExact(date ?? "", "yyyy-MM-dd", out var parsed))
                return InvalidDate();

            var sessions = await sender.Send(new GetSessionsQuery(parsed), ct);
            return Results.Ok(ApiResponse<List<SessionDto>>.Ok(sessions));
        }).WithName("CompanySessions");

        group.MapPost("/sessions/{sessionId:guid}/assign", async (
            Guid sessionId, [FromBody] AssignSessionRequest request, ICurrentUser user,
            TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var session = await sender.Send(
                new AssignSessionResourcesCommand(sessionId, request.BoatId, request.InstructorId), ct);
            return Results.Ok(ApiResponse<SessionDto>.Ok(session, "Seans ataması güncellendi."));
        }).WithName("CompanyAssignSession");

        // ---- Üyeler ve dereceleri ----

        group.MapGet("/customers", async (
            ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var customers = await sender.Send(new GetCustomersQuery(), ct);
            return Results.Ok(ApiResponse<List<CustomerDto>>.Ok(customers));
        }).WithName("CompanyCustomers");

        group.MapPost("/customers", async (
            [FromBody] CreateMemberRequest request, ICurrentUser user, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var customer = await sender.Send(new CreateMemberCommand(
                request.FullName, request.Phone, request.Email, request.Level), ct);
            return Results.Created($"/api/v1/company/customers/{customer.Id}",
                ApiResponse<CustomerDto>.Ok(customer, "Üye eklendi."));
        }).WithName("CompanyCreateMember");

        group.MapPost("/customers/{customerId:guid}/packages", async (
            Guid customerId, [FromBody] AssignPackageRequest request, ICurrentUser user,
            TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var assigned = await sender.Send(new AssignPackageCommand(customerId, request.LessonPackageId), ct);
            return Results.Ok(ApiResponse<RowingClub.Scheduling.Application.Members.CustomerPackageDto>.Ok(
                assigned, "Paket üyeye tanımlandı."));
        }).WithName("CompanyAssignPackage");

        group.MapGet("/customers/{customerId:guid}/packages", async (
            Guid customerId, ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var balances = await sender.Send(new GetCustomerPackagesQuery(customerId), ct);
            return Results.Ok(ApiResponse<List<CustomerPackageBalanceDto>>.Ok(balances));
        }).WithName("CompanyCustomerPackages");

        group.MapGet("/package-balances", async (
            ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var balances = await sender.Send(new GetCustomerPackagesQuery(null), ct);
            return Results.Ok(ApiResponse<List<CustomerPackageBalanceDto>>.Ok(balances));
        }).WithName("CompanyPackageBalances");

        group.MapGet("/customers/{customerId:guid}/logs", async (
            Guid customerId, [FromQuery] int? take, ICurrentUser user, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var logs = await sender.Send(new GetMemberLogsQuery(customerId, take ?? 100), ct);
            return Results.Ok(ApiResponse<List<MemberLogDto>>.Ok(logs));
        }).WithName("CompanyCustomerLogs");

        group.MapGet("/logs", async (
            [FromQuery] int? take, ICurrentUser user, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var logs = await sender.Send(new GetMemberLogsQuery(null, take ?? 100), ct);
            return Results.Ok(ApiResponse<List<MemberLogDto>>.Ok(logs));
        }).WithName("CompanyLogs");

        group.MapGet("/stats", async (
            ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var stats = await sender.Send(new GetCompanyStatsQuery(), ct);
            return Results.Ok(ApiResponse<CompanyStatsDto>.Ok(stats));
        }).WithName("CompanyStats");

        // ---- Randevu tarih yönetimi: kapalı günler ----

        group.MapGet("/closed-dates", async (
            ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var dates = await sender.Send(new GetClosedDatesQuery(), ct);
            return Results.Ok(ApiResponse<List<ClosedDateDto>>.Ok(dates));
        }).WithName("CompanyClosedDates");

        group.MapPost("/closed-dates", async (
            [FromBody] ClosedDateRequest request, ICurrentUser user, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            if (!DateOnly.TryParseExact(request.Date, "yyyy-MM-dd", out var date))
                return InvalidDate();

            var closed = await sender.Send(new AddClosedDateCommand(date, request.Reason), ct);
            return Results.Ok(ApiResponse<ClosedDateDto>.Ok(closed, "Tarih randevuya kapatıldı."));
        }).WithName("CompanyAddClosedDate");

        group.MapDelete("/closed-dates/{date}", async (
            string date, ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsed))
                return InvalidDate();

            await sender.Send(new RemoveClosedDateCommand(parsed), ct);
            return Results.Ok(ApiResponse.Ok("Tarih tekrar randevuya açıldı."));
        }).WithName("CompanyRemoveClosedDate");

        group.MapPost("/customers/{customerId:guid}/level", async (
            Guid customerId, [FromBody] SetCustomerLevelRequest request, ICurrentUser user,
            TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var customer = await sender.Send(new SetCustomerLevelCommand(customerId, request.Level), ct);
            return Results.Ok(ApiResponse<CustomerDto>.Ok(customer, "Üye derecesi güncellendi."));
        }).WithName("CompanySetCustomerLevel");

        // ---- Eğitmenler ----

        group.MapGet("/instructors", async (
            ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var instructors = await sender.Send(new GetInstructorsQuery(), ct);
            return Results.Ok(ApiResponse<List<InstructorDto>>.Ok(instructors));
        }).WithName("CompanyInstructors");

        group.MapPost("/instructors", async (
            [FromBody] InstructorRequest request, ICurrentUser user, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var instructor = await sender.Send(
                new CreateInstructorCommand(request.FullName, request.Phone, request.Email, request.BranchId), ct);
            return Results.Created($"/api/v1/company/instructors/{instructor.Id}",
                ApiResponse<InstructorDto>.Ok(instructor));
        }).WithName("CompanyCreateInstructor");

        group.MapPut("/instructors/{instructorId:guid}", async (
            Guid instructorId, [FromBody] InstructorRequest request, ICurrentUser user,
            TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var instructor = await sender.Send(new UpdateInstructorCommand(
                instructorId, request.FullName, request.Phone, request.Email, request.IsActive ?? true, request.BranchId), ct);
            return Results.Ok(ApiResponse<InstructorDto>.Ok(instructor));
        }).WithName("CompanyUpdateInstructor");

        // ---- Şubeler ----

        group.MapGet("/branches", async (
            ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var branches = await sender.Send(new GetBranchesQuery(), ct);
            return Results.Ok(ApiResponse<List<BranchDto>>.Ok(branches));
        }).WithName("CompanyBranches");

        group.MapPost("/branches", async (
            [FromBody] BranchRequest request, ICurrentUser user, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var branch = await sender.Send(new CreateBranchCommand(request.Name, request.Address, request.Phone), ct);
            return Results.Created($"/api/v1/company/branches/{branch.Id}", ApiResponse<BranchDto>.Ok(branch));
        }).WithName("CompanyCreateBranch");

        group.MapPut("/branches/{branchId:guid}", async (
            Guid branchId, [FromBody] BranchRequest request, ICurrentUser user,
            TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var branch = await sender.Send(new UpdateBranchCommand(
                branchId, request.Name, request.Address, request.Phone, request.IsActive ?? true), ct);
            return Results.Ok(ApiResponse<BranchDto>.Ok(branch));
        }).WithName("CompanyUpdateBranch");

        // ---- Tekneler ----

        group.MapGet("/boats", async (
            ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var boats = await sender.Send(new GetBoatsQuery(), ct);
            return Results.Ok(ApiResponse<List<BoatDto>>.Ok(boats));
        }).WithName("CompanyBoats");

        group.MapPost("/boats", async (
            [FromBody] BoatRequest request, ICurrentUser user, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var boat = await sender.Send(new CreateBoatCommand(request.Name, request.BoatClass, request.BranchId), ct);
            return Results.Created($"/api/v1/company/boats/{boat.Id}", ApiResponse<BoatDto>.Ok(boat));
        }).WithName("CompanyCreateBoat");

        group.MapPut("/boats/{boatId:guid}", async (
            Guid boatId, [FromBody] BoatRequest request, ICurrentUser user,
            TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var boat = await sender.Send(new UpdateBoatCommand(
                boatId, request.Name, request.BoatClass, request.IsActive ?? true, request.BranchId), ct);
            return Results.Ok(ApiResponse<BoatDto>.Ok(boat));
        }).WithName("CompanyUpdateBoat");

        // ---- Ders paketleri ----

        group.MapGet("/packages", async (
            ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var packages = await sender.Send(new GetPackagesQuery(), ct);
            return Results.Ok(ApiResponse<List<PackageDto>>.Ok(packages));
        }).WithName("CompanyPackages");

        group.MapPost("/packages", async (
            [FromBody] PackageRequest request, ICurrentUser user, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var package = await sender.Send(new CreatePackageCommand(
                request.Name, request.Description, request.SessionCount, request.Price), ct);
            return Results.Created($"/api/v1/company/packages/{package.Id}", ApiResponse<PackageDto>.Ok(package));
        }).WithName("CompanyCreatePackage");

        group.MapPut("/packages/{packageId:guid}", async (
            Guid packageId, [FromBody] PackageRequest request, ICurrentUser user,
            TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var package = await sender.Send(new UpdatePackageCommand(
                packageId, request.Name, request.Description, request.SessionCount,
                request.Price, request.IsActive ?? true), ct);
            return Results.Ok(ApiResponse<PackageDto>.Ok(package));
        }).WithName("CompanyUpdatePackage");

        // ---- Firma ayarları (çalışma saatleri, randevu ve hatırlatma kuralları) ----

        group.MapGet("/settings", async (
            ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var settings = await sender.Send(new GetSettingsQuery(), ct);
            return Results.Ok(ApiResponse<CompanySettingsDto>.Ok(settings));
        }).WithName("CompanySettings");

        group.MapPut("/settings", async (
            [FromBody] UpdateSettingsRequest request, ICurrentUser user, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var settings = await sender.Send(new UpdateSettingsCommand(
                request.OpeningTime, request.ClosingTime, request.SlotMinutes, request.OpenDays,
                request.MinNoticeHours, request.MaxAdvanceDays, request.ReminderOptions,
                request.DefaultReminderMinutes, request.TimeZoneId), ct);
            return Results.Ok(ApiResponse<CompanySettingsDto>.Ok(settings, "Ayarlar güncellendi."));
        }).WithName("CompanyUpdateSettings");

        // ---- Kulüp akışı: firma, kulüp adına paylaşır ve akışı yönetir (moderasyon) ----

        group.MapGet("/feed", async (
            ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var posts = await sender.Send(new GetFeedQuery(null, 50), ct);
            return Results.Ok(ApiResponse<List<PostDto>>.Ok(posts));
        }).WithName("CompanyFeed");

        group.MapPost("/feed", async (
            [FromBody] PostCreateRequest request, ICurrentUser user, TenantResolver resolver,
            ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var postId = await sender.Send(new CreatePostCommand(
                null, request.Body, request.MediaBase64, request.MediaContentType,
                request.IsEvent, request.EventTitle, request.EventDate), ct);
            return Results.Ok(ApiResponse<object>.Ok(new { postId }, "Kulüp adına paylaşıldı."));
        }).WithName("CompanyCreatePost");

        group.MapPost("/feed/{postId:guid}/delete", async (
            Guid postId, ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            await sender.Send(new DeletePostCommand(postId, null, IsCompany: true), ct);
            return Results.Ok(ApiResponse.Ok("Paylaşım kaldırıldı."));
        }).WithName("CompanyDeletePost");

        group.MapGet("/feed/{postId:guid}/media", async (
            Guid postId, ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var media = await sender.Send(new GetPostMediaQuery(postId), ct);
            return media is null
                ? Results.NotFound(ApiResponse.Fail("no_media", "Bu paylaşımda medya yok."))
                : Results.Ok(ApiResponse<PostMediaDto>.Ok(media));
        }).WithName("CompanyPostMedia");

        group.MapGet("/feed/{postId:guid}/comments", async (
            Guid postId, ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var comments = await sender.Send(new GetCommentsQuery(postId), ct);
            return Results.Ok(ApiResponse<List<CommentDto>>.Ok(comments));
        }).WithName("CompanyPostComments");

        group.MapGet("/feed/{postId:guid}/participants", async (
            Guid postId, ICurrentUser user, TenantResolver resolver, ISender sender, CancellationToken ct) =>
        {
            if (await ResolveOwnCompanyAsync(user, resolver, ct) is null)
                return CompanyNotFound();

            var participants = await sender.Send(new GetParticipantsQuery(postId), ct);
            return Results.Ok(ApiResponse<List<ParticipantDto>>.Ok(participants));
        }).WithName("CompanyPostParticipants");

        // ---- Firma kullanıcıları (yetkilendirme) ----
        // Handler'lar çağıranın CompanyAdmin olduğunu ve hedefin aynı firmada olduğunu DB'den
        // yeniden doğrular; JWT'deki rol tek başına yeterli görülmez.

        group.MapGet("/users", async (
            ICurrentUser user, ISender sender, CancellationToken ct) =>
        {
            var users = await sender.Send(new GetCompanyUsersQuery(user.UserId), ct);
            return Results.Ok(ApiResponse<List<CompanyUserDto>>.Ok(users));
        }).WithName("CompanyUsers");

        group.MapPost("/users", async (
            [FromBody] CreateCompanyUserRequest request, ICurrentUser user, ISender sender,
            CancellationToken ct) =>
        {
            var created = await sender.Send(
                new CreateCompanyUserCommand(user.UserId, request.Email, request.Password, request.Role), ct);
            return Results.Created($"/api/v1/company/users/{created.Id}",
                ApiResponse<CompanyUserDto>.Ok(created, "Kullanıcı oluşturuldu."));
        }).WithName("CompanyCreateUser");

        group.MapPost("/users/{userId:guid}/role", async (
            Guid userId, [FromBody] ChangeUserRoleRequest request, ICurrentUser user, ISender sender,
            CancellationToken ct) =>
        {
            var updated = await sender.Send(
                new ChangeCompanyUserRoleCommand(user.UserId, userId, request.Role), ct);
            return Results.Ok(ApiResponse<CompanyUserDto>.Ok(updated, "Kullanıcı rolü güncellendi."));
        }).WithName("CompanyChangeUserRole");

        return app;
    }

    private static Task<CompanySiteDto?> ResolveOwnCompanyAsync(
        ICurrentUser user, TenantResolver resolver, CancellationToken cancellationToken)
    {
        return user.CompanyId is { } companyId
            ? resolver.ResolveByCompanyIdAsync(companyId, cancellationToken)
            : Task.FromResult<CompanySiteDto?>(null);
    }

    private static IResult CompanyNotFound() =>
        Results.NotFound(ApiResponse.Fail("company_not_found", "Hesabınıza bağlı aktif bir firma bulunamadı."));

    private static IResult InvalidDate() =>
        Results.BadRequest(ApiResponse.Fail("invalid_date", "Tarih YYYY-AA-GG biçiminde olmalıdır."));
}
