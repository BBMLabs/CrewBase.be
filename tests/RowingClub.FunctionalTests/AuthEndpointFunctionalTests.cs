using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace RowingClub.FunctionalTests;

public sealed class AuthEndpointFunctionalTests(RowingClubWebApplicationFactory factory)
    : IClassFixture<RowingClubWebApplicationFactory>
{
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
    public async Task OpenApi_document_lists_every_auth_endpoint_at_the_v1_route()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        var document = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var paths = document.GetProperty("paths");

        foreach (var expected in new[]
                 {
                     "/api/v1/auth/register", "/api/v1/auth/login", "/api/v1/auth/refresh",
                     "/api/v1/auth/logout", "/api/v1/auth/logout-all",
                 })
        {
            paths.TryGetProperty(expected, out _).Should().BeTrue($"{expected} olmalı");
        }
    }

    [Fact]
    public async Task Auth_endpoints_are_rate_limited_after_the_configured_burst()
    {
        // Deliberately NOT using the shared `factory` fixture: the in-memory rate limiter's
        // counters live for the lifetime of the host, partitioned by (fake, shared-in-TestServer)
        // client IP - reusing the class fixture here would burn through the other tests' quota
        // too. A dedicated factory keeps this test's burst isolated.
        using var isolatedFactory = new RowingClubWebApplicationFactory();
        using var client = isolatedFactory.CreateClient();

        // Empty credentials fail FluentValidation before ever touching Mongo, so this stays fast
        // regardless of database reachability - only the rate limiter's behavior is under test.
        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 15; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "", password = "" });
        }

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
