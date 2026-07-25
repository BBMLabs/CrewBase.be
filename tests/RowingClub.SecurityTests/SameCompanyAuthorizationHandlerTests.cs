using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RowingClub.Api.Security;
using Xunit;


namespace RowingClub.SecurityTests;

public sealed class SameCompanyAuthorizationHandlerTests
{
    private static (SameCompanyAuthorizationHandler handler, AuthorizationHandlerContext ctx, RouteValueDictionary routeValues) CreateFixture(
        string companyIdClaim, string? companyIdRouteValue = null)
    {
        var httpContext = new DefaultHttpContext();
        var routeValues = httpContext.Request.RouteValues;

        if (companyIdRouteValue is not null)
            routeValues["companyId"] = companyIdRouteValue;

        var acc = new FakeHttpContextAccessor(httpContext);

        var handler = new SameCompanyAuthorizationHandler(acc);
        var requirement = new SameCompanyRequirement();

        var identity = new ClaimsIdentity(new[]
        {
            new Claim("company_id", companyIdClaim),
            new Claim(ClaimTypes.Role, "CompanyAdmin"),
        }, "test");
        var principal = new ClaimsPrincipal(identity);
        var ctx = new AuthorizationHandlerContext(new[] { requirement }, principal, new object());

        return (handler, ctx, routeValues);
    }

    [Fact]
    public async Task Succeeds_when_company_id_claim_matches_route()
    {
        var (handler, ctx, _) = CreateFixture("company-a", "company-a");
        await handler.HandleAsync(ctx);
        ctx.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Succeeds_when_company_id_claim_matches_route_case_insensitively()
    {
        var (handler, ctx, _) = CreateFixture("Company-A", "company-a");
        await handler.HandleAsync(ctx);
        ctx.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Fails_when_company_id_claim_does_not_match_route()
    {
        var (handler, ctx, _) = CreateFixture("company-b", "company-a");
        await handler.HandleAsync(ctx);
        ctx.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_when_no_route_value()
    {
        var (handler, ctx, _) = CreateFixture("company-a");
        await handler.HandleAsync(ctx);
        ctx.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_when_no_claim()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["companyId"] = "company-a";
        var acc = new FakeHttpContextAccessor(httpContext);

        var handler = new SameCompanyAuthorizationHandler(acc);
        var requirement = new SameCompanyRequirement();

        var principal = new ClaimsPrincipal(new ClaimsIdentity("test"));
        var ctx = new AuthorizationHandlerContext(new[] { requirement }, principal, new object());

        await handler.HandleAsync(ctx);
        ctx.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Succeeds_when_company_id_claim_matches_case_insensitive_with_different_casing()
    {
        var (handler, ctx, _) = CreateFixture("COMPANY-A", "company-a");
        await handler.HandleAsync(ctx);
        ctx.HasSucceeded.Should().BeTrue();
    }
}
