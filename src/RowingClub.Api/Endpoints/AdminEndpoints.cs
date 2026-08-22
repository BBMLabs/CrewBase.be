using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Identity.Application.Companies.ApproveCompany;
using RowingClub.Identity.Application.Companies.GetPendingCompanies;
using RowingClub.Identity.Application.Companies.SuspendCompany;
using RowingClub.Identity.Application.Platform;

namespace RowingClub.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var platformAdminGroup = app.MapGroup("/api/v1/platform")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "PlatformAdmin" })
            .WithTags("Platform Admin");

        platformAdminGroup.MapGet("/companies", async (IMediator mediator) =>
        {
            var companies = await mediator.Send(new GetAllCompaniesQuery());
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
