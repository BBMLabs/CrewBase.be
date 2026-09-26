using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RowingClub.FunctionalTests;

/// <summary>
/// Regresyon: /member/packages/{packageId}/purchase, handler'ı "id" adında bir Guid bekleyen
/// GuardedRoute'a bağlıydı; rota parametresi adıyla eşleşmeyen zorunlu Guid minimal API'de sorgu
/// dizisinden aranır ve uç her istekte 400/500 döner. Bu test o hata sınıfını tüm uçlarda yakalar.
/// </summary>
public sealed class RouteParameterBindingTests(RowingClubWebApplicationFactory factory)
    : IClassFixture<RowingClubWebApplicationFactory>
{
    [Fact]
    public void Every_required_guid_parameter_is_bound_from_the_route()
    {
        using var _ = factory.CreateClient(); // host'u başlatır
        var dataSource = factory.Services.GetRequiredService<EndpointDataSource>();

        var mismatches = new List<string>();
        foreach (var endpoint in dataSource.Endpoints.OfType<RouteEndpoint>())
        {
            var method = endpoint.Metadata.GetMetadata<MethodInfo>();
            if (method is null)
                continue;

            var routeParameters = endpoint.RoutePattern.Parameters
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var parameter in method.GetParameters())
            {
                if (parameter.ParameterType != typeof(Guid))
                    continue; // Guid? isteğe bağlı sorgu filtreleri (branchId vb.) bilinçli olarak sorgudan gelir

                var explicitSource = parameter.GetCustomAttributes().Any(a =>
                    a is FromQueryAttribute or FromBodyAttribute or FromServicesAttribute or FromHeaderAttribute or FromFormAttribute);
                if (explicitSource)
                    continue;

                if (!routeParameters.Contains(parameter.Name!))
                    mismatches.Add($"{endpoint.RoutePattern.RawText} → Guid {parameter.Name}");
            }
        }

        mismatches.Should().BeEmpty("zorunlu Guid parametreleri rota şablonundaki adla eşleşmeli");
    }
}
