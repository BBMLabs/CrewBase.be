using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace RowingClub.SecurityTests;

/// <summary>Spec section 23 "Güvenlik Testleri" - minimum set for every critical endpoint.</summary>
public sealed class AuthEndpointSecurityTests(RowingClubWebApplicationFactory factory)
    : IClassFixture<RowingClubWebApplicationFactory>
{
    [Fact]
    public async Task LogoutAll_without_a_token_returns_401()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/v1/auth/logout-all", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutAll_with_a_garbage_bearer_token_returns_401_not_500()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-real-jwt");

        var response = await client.PostAsync("/api/v1/auth/logout-all", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_with_malformed_email_returns_400_not_500()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register", new { email = "not-an-email", password = "irrelevant" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_with_sql_injection_style_payload_is_rejected_by_validation_not_executed()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = "'; DROP TABLE users; --@example.com", password = "SecurePass123" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
