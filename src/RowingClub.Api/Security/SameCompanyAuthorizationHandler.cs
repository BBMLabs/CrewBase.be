using Microsoft.AspNetCore.Authorization;

namespace RowingClub.Api.Security;

public sealed class SameCompanyRequirement : IAuthorizationRequirement;

public sealed class SameCompanyAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<SameCompanyRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameCompanyRequirement requirement)
    {
        var userCompanyIdClaim = context.User.FindFirst("company_id")?.Value;

        if (string.IsNullOrWhiteSpace(userCompanyIdClaim))
        {
            return Task.CompletedTask;
        }

        var isPlatformAdmin = context.User.IsInRole("PlatformAdmin");
        if (isPlatformAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return Task.CompletedTask;
        }

        var routeValues = httpContext.Request.RouteValues;

        var companyIdFromRoute = routeValues["companyId"]?.ToString()
            ?? routeValues["companyid"]?.ToString()
            ?? routeValues["company_id"]?.ToString();

        if (string.IsNullOrWhiteSpace(companyIdFromRoute))
        {
            return Task.CompletedTask;
        }

        if (string.Equals(userCompanyIdClaim, companyIdFromRoute, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
