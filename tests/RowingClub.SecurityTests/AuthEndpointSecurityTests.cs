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
        { "/api/v1/platform/companies/pending", HttpMethod.Get },
        { "/api/v1/platform/companies/e9a7e1b1-0000-0000-0000-000000000001/approve", HttpMethod.Post },
        { "/api/v1/platform/companies/e9a7e1b1-0000-0000-0000-000000000001/suspend", HttpMethod.Post },
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
