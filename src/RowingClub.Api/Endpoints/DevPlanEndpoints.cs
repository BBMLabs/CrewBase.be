using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Identity.Application.Companies.SetCompanyPlan;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Api.Endpoints;

public sealed record DevPlanSwitchRequest(string Plan);

public static class DevPlanEndpoints
{
    public static IEndpointRouteBuilder MapDevPlanEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/company/plan/dev-switch", async (
            [FromBody] DevPlanSwitchRequest request, ICurrentUser user, ISender sender, CancellationToken ct) =>
        {
            if (user.CompanyId is not { } companyId)
                return Results.Json(ApiResponse.Fail("forbidden", "Bu işlem için firma hesabı gerekir."), statusCode: StatusCodes.Status403Forbidden);

            if (!Enum.TryParse<CompanyPlan>(request.Plan, ignoreCase: true, out var plan) || plan == CompanyPlan.Custom)
                return Results.BadRequest(ApiResponse.Fail("invalid_plan", "Geçersiz paket."));

            await sender.Send(new SetCompanyPlanCommand(companyId, plan.ToString(), null, null, null, null), ct);
            return Results.Ok(ApiResponse.Ok($"Paket {plan} olarak değiştirildi (geliştirme ortamı)."));
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = "CompanyAdmin" })
        .WithName("CompanyPlanDevSwitch");

        return app;
    }
}
