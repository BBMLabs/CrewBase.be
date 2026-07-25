using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace RowingClub.FunctionalTests;

public sealed class AuthEndpointFunctionalTests(RowingClubWebApplicationFactory factory)
    : IClassFixture<RowingClubWebApplicationFactory>
{
    [Fact]
    public async Task OpenApi_document_lists_every_auth_endpoint_at_the_v1_route()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        var document = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var paths = document.GetProperty("paths");

        foreach (var expected in new[]
                 {
                     "/api/v1/auth/register",
                     "/api/v1/auth/companies/register",
                     "/api/v1/auth/login",
                     "/api/v1/auth/login/verify-2fa",
                     "/api/v1/auth/refresh",
                     "/api/v1/auth/logout",
                     "/api/v1/auth/logout-all",
                     "/api/v1/auth/forgot-password",
                     "/api/v1/auth/reset-password",
                     "/api/v1/auth/verify-email",
                     "/api/v1/auth/send-verification-email",
                     "/api/v1/auth/2fa/setup",
                     "/api/v1/auth/2fa/enable",
                     "/api/v1/auth/2fa/disable",
                     "/api/v1/auth/2fa/recovery-codes",
                 })
        {
            paths.TryGetProperty(expected, out _).Should().BeTrue($"{expected} olmalı");
        }
    }

    [Fact]
    public async Task OpenApi_document_lists_every_admin_endpoint()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        var document = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var paths = document.GetProperty("paths");

        foreach (var expected in new[]
                 {
                     "/api/v1/platform/companies/pending",
                     "/api/v1/platform/companies/{companyId}/approve",
                     "/api/v1/platform/companies/{companyId}/suspend",
                     "/api/v1/admin/{companyId}/dashboard",
                     "/api/v1/admin/{companyId}/users",
                     "/api/v1/admin/profile",
                     "/api/v1/admin/employee/tasks",
                 })
        {
            paths.TryGetProperty(expected, out _).Should().BeTrue($"{expected} OpenAPI'de eksik");
        }
    }

    [Fact]
    public async Task Register_validation_failure_returns_problem_details_with_correlation_id()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register", new { email = "not-an-email", password = "short" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey("X-Correlation-Id");

        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("code").GetString().Should().Be("validation_error");
        body.GetProperty("errors").TryGetProperty("Email", out _).Should().BeTrue();
        body.GetProperty("errors").TryGetProperty("Password", out _).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterCompany_validation_failure_returns_problem_details()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/companies/register", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("code").GetString().Should().Be("validation_error");
        body.GetProperty("errors").TryGetProperty("CompanyName", out _).Should().BeTrue();
        body.GetProperty("errors").TryGetProperty("AdminEmail", out _).Should().BeTrue();
        body.GetProperty("errors").TryGetProperty("AdminPassword", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPassword_validation_failure_returns_problem_details()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/forgot-password", new { email = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("code").GetString().Should().Be("validation_error");
    }

    [Fact]
    public async Task VerifyTwoFactor_validation_failure_returns_problem_details()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login/verify-2fa", new { PendingToken = "", Code = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("code").GetString().Should().Be("validation_error");
    }

    [Fact]
    public async Task ResetPassword_validation_failure_returns_problem_details()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/reset-password", new { email = "", token = "", newPassword = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("code").GetString().Should().Be("validation_error");
    }

    [Fact]
    public async Task VerifyEmail_validation_failure_returns_problem_details()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/verify-email", new { email = "", token = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("code").GetString().Should().Be("validation_error");
    }

    [Fact]
    public async Task SendVerificationEmail_validation_failure_returns_problem_details()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/send-verification-email", new { email = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("code").GetString().Should().Be("validation_error");
    }

    [Fact]
    public async Task Correlation_id_sent_by_the_client_is_echoed_back_unchanged()
    {
        using var client = factory.CreateClient();
        const string correlationId = "test-correlation-12345";
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register", new { email = "not-an-email", password = "short" });

        response.Headers.GetValues("X-Correlation-Id").Should().ContainSingle().Which.Should().Be(correlationId);
    }

    [Fact]
    public async Task Auth_endpoints_are_rate_limited_after_the_configured_burst()
    {
        using var isolatedFactory = new RowingClubWebApplicationFactory();
        using var client = isolatedFactory.CreateClient();

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 15; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "", password = "" });
        }

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
