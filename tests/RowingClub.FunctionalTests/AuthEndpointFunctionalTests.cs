using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using RowingClub.Identity.Domain.Companies;

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
                     "/api/v1/auth/companies/register",
                     "/api/v1/auth/companies/subdomain-availability",
                     "/api/v1/auth/login",
                     "/api/v1/auth/refresh",
                     "/api/v1/auth/logout",
                     "/api/v1/auth/logout-all",
                     "/api/v1/auth/forgot-password",
                     "/api/v1/auth/reset-password",
                     "/api/v1/auth/verify-email",
                     "/api/v1/auth/send-verification-email",
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
                     "/api/v1/platform/activity-logs",
                     "/api/v1/platform/revenue",
                     "/api/v1/platform/payments",
                     "/api/v1/platform/payments/stats",
                     "/api/v1/platform/companies/{companyId}/subscription",
                     "/api/v1/platform/dev/wipe-all-data",
                     "/api/v1/platform/databases",
                     "/api/v1/platform/databases/{databaseName}/drop",
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
    public async Task OpenApi_document_lists_the_public_rsvp_endpoints()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        var document = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var paths = document.GetProperty("paths");

        paths.TryGetProperty("/api/v1/public/{subdomain}/rsvp/{token}", out var rsvp).Should().BeTrue();
        rsvp.TryGetProperty("get", out _).Should().BeTrue();
        rsvp.TryGetProperty("post", out _).Should().BeTrue();
    }

    [Fact]
    public async Task OpenApi_document_lists_the_public_seo_endpoints()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        var document = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var paths = document.GetProperty("paths");

        foreach (var path in new[]
                 {
                     "/api/v1/public/{subdomain}/sitemap.xml",
                     "/api/v1/public/{subdomain}/robots.txt",
                 })
        {
            paths.TryGetProperty(path, out var item).Should().BeTrue($"{path} OpenAPI'de eksik");
            item.TryGetProperty("get", out var operation).Should().BeTrue($"GET {path} OpenAPI'de eksik");
            operation.TryGetProperty("security", out _).Should().BeFalse($"{path} anonim olmalı");
        }
    }

    [Fact]
    public async Task OpenApi_document_lists_the_feed_interaction_endpoints()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        var document = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var paths = document.GetProperty("paths");

        foreach (var (path, method) in new[]
                 {
                     ("/api/v1/company/feed/{postId}/react", "post"),
                     ("/api/v1/company/feed/{postId}/reactions", "get"),
                     ("/api/v1/company/feed/{postId}/likes", "get"),
                     ("/api/v1/company/feed/{postId}/comments", "post"),
                     ("/api/v1/company/feed/{postId}/media/{index}", "get"),
                     ("/api/v1/company/feed/comments/{commentId}/like", "post"),
                     ("/api/v1/company/feed/comments/{commentId}/delete", "post"),
                     ("/api/v1/member/feed/{id}/react", "post"),
                     ("/api/v1/member/feed/{id}/like", "post"),
                     ("/api/v1/member/feed/{id}/media/{index}", "get"),
                     ("/api/v1/member/feed/comments/{id}/like", "post"),
                     ("/api/v1/member/feed/comments/{id}/delete", "post"),
                     ("/api/v1/public/{subdomain}/feed/{postId}/media/{index}", "get"),
                 })
        {
            paths.TryGetProperty(path, out var item).Should().BeTrue($"{path} OpenAPI'de eksik");
            item.TryGetProperty(method, out _).Should().BeTrue($"{method.ToUpperInvariant()} {path} OpenAPI'de eksik");
        }
    }

    [Fact]
    public async Task Rsvp_with_an_invalid_choice_returns_400_before_touching_the_tenant()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/public/demo/rsvp/some-token", new { choice = "maybe" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("code").GetString().Should().Be("rsvp_invalid_choice");
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
        body.GetProperty("errors").TryGetProperty("TaxNumber", out _).Should().BeTrue();
        body.GetProperty("errors").TryGetProperty("Phone", out _).Should().BeTrue();
        body.GetProperty("errors").TryGetProperty("ContactEmail", out _).Should().BeTrue();
        body.GetProperty("errors").TryGetProperty("Address", out _).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterCompany_with_reserved_subdomain_returns_problem_details()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/companies/register", new
        {
            companyName = "Test Kulübü",
            adminEmail = "admin@example.com",
            taxNumber = "12345678901",
            phone = "+905551234567",
            contactEmail = "iletisim@example.com",
            address = "İstanbul",
            subdomain = "admin",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("code").GetString().Should().Be("validation_error");
        body.GetProperty("errors").TryGetProperty("Subdomain", out _).Should().BeTrue();
    }

    [Fact]
    public async Task SubdomainAvailability_returns_true_for_a_fresh_valid_value()
    {
        // Fonksiyonel test host'unun gerçek bir veritabanı yok (Postgres host'u 'unused'); müsaitlik
        // sorgusu DB'ye gittiği için yalnızca repository sahte nesneyle değiştirilir - HTTP sözleşmesi
        // ve handler'ın slug/rezerve kontrolleri gerçek koddan geçer.
        var companies = Substitute.For<ICompanyRepository>();
        companies.ExistsBySubdomainAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        using var client = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICompanyRepository>();
            services.AddScoped(_ => companies);
        })).CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/auth/companies/subdomain-availability?value=fresh-club-{Guid.NewGuid():N}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("data").GetProperty("available").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SubdomainAvailability_without_value_is_a_client_error_not_500()
    {
        // Regresyon: eksik zorunlu query parametresi GlobalExceptionHandler'da 500 unexpected_error oluyordu.
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/companies/subdomain-availability");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubdomainAvailability_returns_false_for_a_reserved_value()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/companies/subdomain-availability?value=admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("data").GetProperty("available").GetBoolean().Should().BeFalse();
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
    public async Task TwoFactor_endpoints_are_disabled()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login/verify-2fa", new { PendingToken = "x", Code = "123456" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
            "/api/v1/auth/forgot-password", new { email = "" });

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
