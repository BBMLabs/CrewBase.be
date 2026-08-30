using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace RowingClub.SecurityTests;

public sealed class AuthEndpointSecurityTests(RowingClubWebApplicationFactory factory)
    : IClassFixture<RowingClubWebApplicationFactory>
{
    public static TheoryData<string, HttpMethod> ProtectedEndpoints => new()
    {
        { "/api/v1/auth/logout-all", HttpMethod.Post },
        { "/api/v1/company/appointments", HttpMethod.Get },
        { "/api/v1/company/customers", HttpMethod.Get },
        { "/api/v1/company/customers/e9a7e1b1-0000-0000-0000-000000000001/send-password-reset", HttpMethod.Post },
        { "/api/v1/company/branches/e9a7e1b1-0000-0000-0000-000000000001", HttpMethod.Delete },
        { "/api/v1/company/branches/e9a7e1b1-0000-0000-0000-000000000001/members/export", HttpMethod.Get },
        { "/api/v1/company/packages/e9a7e1b1-0000-0000-0000-000000000001", HttpMethod.Delete },
        { "/api/v1/company/boats/e9a7e1b1-0000-0000-0000-000000000001", HttpMethod.Delete },
        { "/api/v1/company/instructors/e9a7e1b1-0000-0000-0000-000000000001", HttpMethod.Delete },
        { "/api/v1/company/site", HttpMethod.Get },
        { "/api/v1/company/plan", HttpMethod.Get },
        { "/api/v1/company/plan/payments", HttpMethod.Get },
        { "/api/v1/company/plan/subscribe", HttpMethod.Post },
        { "/api/v1/company/plan/checkout-result", HttpMethod.Post },
        { "/api/v1/company/plan/upgrade", HttpMethod.Post },
        { "/api/v1/company/plan/downgrade", HttpMethod.Post },
        { "/api/v1/company/plan/downgrade/cancel", HttpMethod.Post },
        { "/api/v1/company/appointments/e9a7e1b1-0000-0000-0000-000000000001/status", HttpMethod.Post },
        { "/api/v1/company/sessions", HttpMethod.Get },
        { "/api/v1/company/sessions/e9a7e1b1-0000-0000-0000-000000000001/assign", HttpMethod.Post },
        { "/api/v1/company/customers", HttpMethod.Post },
        { "/api/v1/company/customers/e9a7e1b1-0000-0000-0000-000000000001/branch", HttpMethod.Post },
        { "/api/v1/company/customers/e9a7e1b1-0000-0000-0000-000000000001/block", HttpMethod.Post },
        { "/api/v1/company/customers/e9a7e1b1-0000-0000-0000-000000000001/unblock", HttpMethod.Post },
        { "/api/v1/company/customers/e9a7e1b1-0000-0000-0000-000000000001/packages", HttpMethod.Post },
        { "/api/v1/company/customers/e9a7e1b1-0000-0000-0000-000000000001/packages", HttpMethod.Get },
        { "/api/v1/company/package-balances", HttpMethod.Get },
        { "/api/v1/company/customers/e9a7e1b1-0000-0000-0000-000000000001/logs", HttpMethod.Get },
        { "/api/v1/company/logs", HttpMethod.Get },
        { "/api/v1/company/activity-logs", HttpMethod.Get },
        { "/api/v1/company/stats", HttpMethod.Get },
        { "/api/v1/company/insights", HttpMethod.Get },
        { "/api/v1/company/closed-dates", HttpMethod.Get },
        { "/api/v1/company/closed-dates", HttpMethod.Post },
        { "/api/v1/company/closed-dates/2025-01-01", HttpMethod.Delete },
        { "/api/v1/company/customers/e9a7e1b1-0000-0000-0000-000000000001/level", HttpMethod.Post },
        { "/api/v1/company/instructors", HttpMethod.Get },
        { "/api/v1/company/instructors", HttpMethod.Post },
        { "/api/v1/company/instructors/e9a7e1b1-0000-0000-0000-000000000001", HttpMethod.Put },
        { "/api/v1/company/branches", HttpMethod.Get },
        { "/api/v1/company/branches", HttpMethod.Post },
        { "/api/v1/company/branches/e9a7e1b1-0000-0000-0000-000000000001", HttpMethod.Put },
        { "/api/v1/company/branches/e9a7e1b1-0000-0000-0000-000000000001/logo", HttpMethod.Post },
        { "/api/v1/company/branches/e9a7e1b1-0000-0000-0000-000000000001/detail", HttpMethod.Get },
        { "/api/v1/company/boats", HttpMethod.Get },
        { "/api/v1/company/boats", HttpMethod.Post },
        { "/api/v1/company/boats/e9a7e1b1-0000-0000-0000-000000000001", HttpMethod.Put },
        { "/api/v1/company/packages", HttpMethod.Get },
        { "/api/v1/company/packages", HttpMethod.Post },
        { "/api/v1/company/packages/e9a7e1b1-0000-0000-0000-000000000001", HttpMethod.Put },
        { "/api/v1/company/packages/e9a7e1b1-0000-0000-0000-000000000001/image", HttpMethod.Post },
        { "/api/v1/company/settings", HttpMethod.Get },
        { "/api/v1/company/settings", HttpMethod.Put },
        { "/api/v1/company/feed", HttpMethod.Get },
        { "/api/v1/company/feed", HttpMethod.Post },
        { "/api/v1/company/feed/e9a7e1b1-0000-0000-0000-000000000001/delete", HttpMethod.Post },
        { "/api/v1/company/feed/e9a7e1b1-0000-0000-0000-000000000001/media", HttpMethod.Get },
        { "/api/v1/company/feed/e9a7e1b1-0000-0000-0000-000000000001/comments", HttpMethod.Get },
        { "/api/v1/company/feed/e9a7e1b1-0000-0000-0000-000000000001/participants", HttpMethod.Get },
        { "/api/v1/company/users", HttpMethod.Get },
        { "/api/v1/company/users", HttpMethod.Post },
        { "/api/v1/company/users/e9a7e1b1-0000-0000-0000-000000000001/role", HttpMethod.Post },
        { "/api/v1/company/users/e9a7e1b1-0000-0000-0000-000000000001/block", HttpMethod.Post },
        { "/api/v1/company/users/e9a7e1b1-0000-0000-0000-000000000001/unblock", HttpMethod.Post },
        { "/api/v1/company/blocked-ips", HttpMethod.Get },
        { "/api/v1/company/blocked-ips", HttpMethod.Post },
        { "/api/v1/company/blocked-ips/1.2.3.4", HttpMethod.Delete },
        { "/api/v1/member/me", HttpMethod.Get },
        { "/api/v1/member/me", HttpMethod.Put },
        { "/api/v1/member/me", HttpMethod.Delete },
        { "/api/v1/member/otp/request", HttpMethod.Post },
        { "/api/v1/member/otp/verify", HttpMethod.Post },
        { "/api/v1/member/appointments", HttpMethod.Get },
        { "/api/v1/member/appointments", HttpMethod.Post },
        { "/api/v1/member/appointments/e9a7e1b1-0000-0000-0000-000000000001/cancel", HttpMethod.Post },
        { "/api/v1/member/packages", HttpMethod.Get },
        { "/api/v1/member/packages/catalog", HttpMethod.Get },
        { "/api/v1/member/packages/e9a7e1b1-0000-0000-0000-000000000001/purchase", HttpMethod.Post },
        { "/api/v1/member/packages/purchase/checkout-result", HttpMethod.Post },
        { "/api/v1/member/consents", HttpMethod.Get },
        { "/api/v1/member/consents", HttpMethod.Post },
        { "/api/v1/member/cards", HttpMethod.Get },
        { "/api/v1/member/cards", HttpMethod.Put },
        { "/api/v1/member/code", HttpMethod.Get },
        { "/api/v1/member/friends", HttpMethod.Get },
        { "/api/v1/member/friends", HttpMethod.Post },
        { "/api/v1/member/friends/e9a7e1b1-0000-0000-0000-000000000001/accept", HttpMethod.Post },
        { "/api/v1/member/friends/e9a7e1b1-0000-0000-0000-000000000001/reject", HttpMethod.Post },
        { "/api/v1/member/messages/e9a7e1b1-0000-0000-0000-000000000001", HttpMethod.Get },
        { "/api/v1/member/messages/e9a7e1b1-0000-0000-0000-000000000001", HttpMethod.Post },
        { "/api/v1/member/feed", HttpMethod.Get },
        { "/api/v1/member/feed", HttpMethod.Post },
        { "/api/v1/member/feed/e9a7e1b1-0000-0000-0000-000000000001/media", HttpMethod.Get },
        { "/api/v1/member/feed/e9a7e1b1-0000-0000-0000-000000000001/delete", HttpMethod.Post },
        { "/api/v1/member/feed/e9a7e1b1-0000-0000-0000-000000000001/like", HttpMethod.Post },
        { "/api/v1/member/feed/e9a7e1b1-0000-0000-0000-000000000001/comments", HttpMethod.Get },
        { "/api/v1/member/feed/e9a7e1b1-0000-0000-0000-000000000001/comments", HttpMethod.Post },
        { "/api/v1/member/feed/e9a7e1b1-0000-0000-0000-000000000001/join", HttpMethod.Post },
        { "/api/v1/member/feed/e9a7e1b1-0000-0000-0000-000000000001/participants", HttpMethod.Get },
        { "/api/v1/member/follow/e9a7e1b1-0000-0000-0000-000000000001", HttpMethod.Post },
        { "/api/v1/platform/companies/pending", HttpMethod.Get },
        { "/api/v1/platform/companies/e9a7e1b1-0000-0000-0000-000000000001/approve", HttpMethod.Post },
        { "/api/v1/platform/companies/e9a7e1b1-0000-0000-0000-000000000001/suspend", HttpMethod.Post },
        { "/api/v1/platform/companies", HttpMethod.Get },
        { "/api/v1/platform/stats", HttpMethod.Get },
        { "/api/v1/platform/companies/e9a7e1b1-0000-0000-0000-000000000001", HttpMethod.Put },
        { "/api/v1/platform/companies/e9a7e1b1-0000-0000-0000-000000000001/plan", HttpMethod.Post },
        { "/api/v1/platform/companies/e9a7e1b1-0000-0000-0000-000000000001/delete", HttpMethod.Post },
        { "/api/v1/platform/companies/e9a7e1b1-0000-0000-0000-000000000001/restore", HttpMethod.Post },
        { "/api/v1/platform/companies/e9a7e1b1-0000-0000-0000-000000000001/reset-password", HttpMethod.Post },
        { "/api/v1/platform/companies/e9a7e1b1-0000-0000-0000-000000000001/overview", HttpMethod.Get },
        { "/api/v1/platform/activity-logs", HttpMethod.Get },
        { "/api/v1/platform/revenue", HttpMethod.Get },
        { "/api/v1/platform/companies/e9a7e1b1-0000-0000-0000-000000000001/subscription", HttpMethod.Get },
        { "/api/v1/platform/companies/e9a7e1b1-0000-0000-0000-000000000001/subscription/cancel", HttpMethod.Post },
        { "/api/v1/platform/companies/e9a7e1b1-0000-0000-0000-000000000001/payments/manual-correction", HttpMethod.Post },
        { "/api/v1/admin/e9a7e1b1-0000-0000-0000-000000000001/dashboard", HttpMethod.Get },
        { "/api/v1/admin/e9a7e1b1-0000-0000-0000-000000000001/users", HttpMethod.Get },
        { "/api/v1/admin/profile", HttpMethod.Get },
        { "/api/v1/admin/employee/tasks", HttpMethod.Get },
    };

    public static TheoryData<string, object> PublicEndpointsWithInvalidPayload => new()
    {
        { "/api/v1/auth/companies/register", new { } },
        { "/api/v1/auth/login", new { email = "", password = "" } },
        { "/api/v1/auth/refresh", new { RefreshToken = "" } },
        { "/api/v1/auth/logout", new { RefreshToken = "" } },
        { "/api/v1/auth/forgot-password", new { email = "not-an-email" } },
        { "/api/v1/auth/reset-password", new { email = "", token = "", newPassword = "" } },
        { "/api/v1/auth/verify-email", new { email = "", token = "" } },
        { "/api/v1/auth/send-verification-email", new { email = "" } },
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task Protected_endpoint_returns_401_without_auth(string url, HttpMethod method)
    {
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(method, url);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task Protected_endpoint_returns_401_with_garbage_token(string url, HttpMethod method)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-real-jwt");

        using var request = new HttpRequestMessage(method, url)
        {
            Content = url.Contains("enable", StringComparison.OrdinalIgnoreCase)
                ? JsonContent.Create(new { Method = "Totp", Code = "123456" })
                : url.Contains("disable", StringComparison.OrdinalIgnoreCase)
                    ? JsonContent.Create(new { Password = "irrelevant" })
                    : null,
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(PublicEndpointsWithInvalidPayload))]
    public async Task Public_endpoint_returns_400_with_invalid_input(string url, object payload)
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(url, payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task Protected_endpoint_does_not_return_500(string url, HttpMethod method)
    {
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(method, url);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Theory]
    [MemberData(nameof(PublicEndpointsWithInvalidPayload))]
    public async Task Public_endpoint_does_not_return_500_with_garbage_input(string url, object payload)
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(url, payload);

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Login_with_empty_credentials_returns_400_not_401()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "", password = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Health_endpoint_requires_no_authentication()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
