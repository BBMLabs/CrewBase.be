using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Identity.Application.Companies.ApproveCompany;
using RowingClub.Identity.Application.Companies.Billing.CancelSubscription;
using RowingClub.Identity.Application.Companies.Billing.RecordManualPaymentCorrection;
using RowingClub.Identity.Application.Companies.GetPendingCompanies;
using RowingClub.Identity.Application.Companies.ResetCompanyAdminPassword;
using RowingClub.Identity.Application.Companies.RestoreCompany;
using RowingClub.Identity.Application.Companies.SetCompanyPlan;
using RowingClub.Identity.Application.Companies.SoftDeleteCompany;
using RowingClub.Identity.Application.Companies.SuspendCompany;
using RowingClub.Identity.Application.Companies.UpdateCompany;
using RowingClub.Identity.Application.Platform;

namespace RowingClub.Api.Endpoints;

public sealed record UpdateCompanyRequest(string Name, string? Phone, string? ContactEmail, string? Address, string? TaxNumber);
public sealed record SetCompanyPlanRequest(
    string Plan, int? CustomMaxBranches, int? CustomMaxMembers, int? CustomMaxBoats, int? CustomMaxInstructors,
    int? CustomMaxManagers, int? CustomMaxEmployees, bool? CustomCanExportData, bool? CustomHasAdvancedReports,
    bool? CustomHasAutomaticDuesReminders);
public sealed record RecordManualPaymentCorrectionRequest(
    decimal Amount, string Currency, string Kind, string Status, string Note);

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var platformAdminGroup = app.MapGroup("/api/v1/platform")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "PlatformAdmin" })
            .WithTags("Platform Admin");

        platformAdminGroup.MapGet("/companies", async (
            bool? includeDeleted, string? search, string? status, string? sortBy, bool? sortDescending,
            IMediator mediator) =>
        {
            var companies = await mediator.Send(new GetAllCompaniesQuery(
                includeDeleted ?? false, search, status, sortBy, sortDescending ?? true));
            return Results.Ok(ApiResponse<List<PlatformCompanyDto>>.Ok(companies));
        })
        .WithName("GetAllCompanies");

        platformAdminGroup.MapGet("/stats", async (IMediator mediator) =>
        {
            var stats = await mediator.Send(new GetPlatformStatsQuery());
            return Results.Ok(ApiResponse<PlatformStatsDto>.Ok(stats));
        })
        .WithName("GetPlatformStats");

        platformAdminGroup.MapGet("/companies/pending", async (IMediator mediator) =>
        {
            var companies = await mediator.Send(new GetPendingCompaniesQuery());
            return Results.Ok(ApiResponse<List<PendingCompanyDto>>.Ok(companies));
        })
        .WithName("GetPendingCompanies");

        platformAdminGroup.MapPost("/companies/{companyId:guid}/approve", async (
            Guid companyId, [FromServices] IMediator mediator, [FromServices] ICurrentUser user) =>
        {
            await mediator.Send(new ApproveCompanyCommand(companyId, user.UserId));
            return Results.Ok(ApiResponse.Ok("Şirket onaylandı."));
        })
        .WithName("ApproveCompany");

        platformAdminGroup.MapPost("/companies/{companyId:guid}/suspend", async (
            Guid companyId, [FromServices] IMediator mediator) =>
        {
            await mediator.Send(new SuspendCompanyCommand(companyId));
            return Results.Ok(ApiResponse.Ok("Şirket askıya alındı."));
        })
        .WithName("SuspendCompany");

        platformAdminGroup.MapPut("/companies/{companyId:guid}", async (
            Guid companyId, [FromBody] UpdateCompanyRequest request, [FromServices] IMediator mediator) =>
        {
            await mediator.Send(new UpdateCompanyCommand(
                companyId, request.Name, request.Phone, request.ContactEmail, request.Address, request.TaxNumber));
            return Results.Ok(ApiResponse.Ok("Şirket bilgileri güncellendi."));
        })
        .WithName("UpdateCompany");

        platformAdminGroup.MapPost("/companies/{companyId:guid}/plan", async (
            Guid companyId, [FromBody] SetCompanyPlanRequest request, [FromServices] IMediator mediator) =>
        {
            await mediator.Send(new SetCompanyPlanCommand(
                companyId, request.Plan, request.CustomMaxBranches, request.CustomMaxMembers, request.CustomMaxBoats,
                request.CustomMaxInstructors, request.CustomMaxManagers, request.CustomMaxEmployees,
                request.CustomCanExportData, request.CustomHasAdvancedReports, request.CustomHasAutomaticDuesReminders));
            return Results.Ok(ApiResponse.Ok("Şirket paketi güncellendi."));
        })
        .WithName("SetCompanyPlan");

        platformAdminGroup.MapPost("/companies/{companyId:guid}/delete", async (
            Guid companyId, [FromServices] IMediator mediator) =>
        {
            await mediator.Send(new SoftDeleteCompanyCommand(companyId));
            return Results.Ok(ApiResponse.Ok("Şirket silindi."));
        })
        .WithName("SoftDeleteCompany");

        platformAdminGroup.MapPost("/companies/{companyId:guid}/restore", async (
            Guid companyId, [FromServices] IMediator mediator) =>
        {
            await mediator.Send(new RestoreCompanyCommand(companyId));
            return Results.Ok(ApiResponse.Ok("Şirket geri yüklendi."));
        })
        .WithName("RestoreCompany");

        platformAdminGroup.MapPost("/companies/{companyId:guid}/reset-password", async (
            Guid companyId, [FromServices] IMediator mediator) =>
        {
            await mediator.Send(new ResetCompanyAdminPasswordCommand(companyId));
            return Results.Ok(ApiResponse.Ok("Parola sıfırlama bağlantısı gönderildi."));
        })
        .WithName("ResetCompanyAdminPassword");

        platformAdminGroup.MapGet("/activity-logs", async (
            string? search, string? cursor, int? limit, IMediator mediator) =>
        {
            var logs = await mediator.Send(new GetPlatformActivityLogsQuery(search, cursor, limit ?? 25));
            return Results.Ok(ApiResponse<KeysetResult<PlatformActivityLogDto>>.Ok(logs));
        })
        .WithName("GetPlatformActivityLogs");

        platformAdminGroup.MapGet("/revenue", async (IMediator mediator) =>
        {
            var revenue = await mediator.Send(new GetPlatformRevenueQuery());
            return Results.Ok(ApiResponse<PlatformRevenueDto>.Ok(revenue));
        })
        .WithName("GetPlatformRevenue");

        platformAdminGroup.MapGet("/payments", async (
            string? status, string? cursor, int? limit, IMediator mediator) =>
        {
            var payments = await mediator.Send(new GetPlatformPaymentsQuery(status, cursor, limit ?? 25));
            return Results.Ok(ApiResponse<KeysetResult<PlatformPaymentDto>>.Ok(payments));
        })
        .WithName("GetPlatformPayments");

        platformAdminGroup.MapGet("/payments/stats", async (IMediator mediator) =>
        {
            var stats = await mediator.Send(new GetPlatformPaymentStatsQuery());
            return Results.Ok(ApiResponse<PlatformPaymentStatsDto>.Ok(stats));
        })
        .WithName("GetPlatformPaymentStats");

        platformAdminGroup.MapGet("/companies/{companyId:guid}/subscription", async (
            Guid companyId, string? cursor, int? limit, IMediator mediator) =>
        {
            var subscription = await mediator.Send(new GetCompanySubscriptionQuery(companyId, cursor, limit ?? 25));
            return Results.Ok(ApiResponse<CompanySubscriptionDetailDto>.Ok(subscription));
        })
        .WithName("GetCompanySubscription");

        platformAdminGroup.MapPost("/companies/{companyId:guid}/subscription/cancel", async (
            Guid companyId, [FromServices] IMediator mediator) =>
        {
            await mediator.Send(new CancelCompanySubscriptionCommand(companyId));
            return Results.Ok(ApiResponse.Ok("Abonelik iptal edildi."));
        })
        .WithName("CancelCompanySubscription");

        platformAdminGroup.MapPost("/companies/{companyId:guid}/payments/manual-correction", async (
            Guid companyId, [FromBody] RecordManualPaymentCorrectionRequest request, [FromServices] IMediator mediator) =>
        {
            await mediator.Send(new RecordManualPaymentCorrectionCommand(
                companyId, request.Amount, request.Currency, request.Kind, request.Status, request.Note));
            return Results.Ok(ApiResponse.Ok("Manuel ödeme kaydı eklendi."));
        })
        .WithName("RecordManualPaymentCorrection");

        platformAdminGroup.MapGet("/companies/{companyId:guid}/overview", async (
            Guid companyId, RowingClub.Api.Tenancy.TenantResolver resolver, IMediator mediator, CancellationToken ct) =>
        {
            var company = await resolver.ResolveByCompanyIdAsync(companyId, ct);
            if (company is null)
                return Results.NotFound(ApiResponse.Fail("company_not_found", "Firma bulunamadı."));

            var overview = await mediator.Send(
                new RowingClub.Scheduling.Application.Panel.GetCompanyOverviewQuery(), ct);
            return Results.Ok(ApiResponse<RowingClub.Scheduling.Application.Panel.CompanyOverviewDto>.Ok(overview));
        })
        .WithName("PlatformCompanyOverview");

        var companyGroup = app.MapGroup("/api/v1/admin/{companyId:guid}")
            .RequireAuthorization(new AuthorizeAttribute { Policy = "SameCompany" })
            .WithTags("Admin");

        companyGroup.MapGet("/dashboard", async (Guid companyId, ICurrentUser user) =>
            Results.Ok(ApiResponse.Ok($"Hoş geldin {user.Email}. {companyId} şirket panosuna erişiminiz var.")))
            .RequireAuthorization(new AuthorizeAttribute { Roles = "CompanyAdmin" })
            .WithName("AdminDashboard");

        companyGroup.MapGet("/users", async (Guid companyId, ICurrentUser user) =>
            Results.Ok(ApiResponse.Ok($"Çalışan listesi. {user.Email} tarafından erişildi.")))
            .RequireAuthorization(new AuthorizeAttribute { Roles = "CompanyAdmin" })
            .WithName("AdminListUsers");

        var userGroup = app.MapGroup("/api/v1/admin")
            .RequireAuthorization()
            .WithTags("Admin");

        userGroup.MapGet("/profile", async (ICurrentUser user) =>
            Results.Ok(ApiResponse.Ok($"Profil bilgileriniz. Email: {user.Email}, Rol: {user.Role}")))
            .WithName("UserProfile");

        userGroup.MapGet("/employee/tasks", async (ICurrentUser user) =>
            Results.Ok(ApiResponse.Ok($"Görev listeniz. {user.Email} için yükleniyor...")))
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Employee" })
            .WithName("EmployeeTasks");

        return app;
    }
}
