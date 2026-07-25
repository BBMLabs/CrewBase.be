using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace RowingClub.ArchitectureTests;

/// <summary>
/// Enforces spec section 23 "Mimari Test" rules:
/// - Domain has no dependency on Infrastructure or Application.
/// - Application has no dependency on Infrastructure.
/// - A module's Infrastructure never depends directly on another module's Infrastructure.
/// </summary>
public sealed class LayeringTests
{
    public static TheoryData<string, Assembly> DomainAssemblies { get; } = new()
    {
        { "BuildingBlocks.Domain", typeof(RowingClub.BuildingBlocks.Domain.Entity<>).Assembly },
        { "Identity.Domain", typeof(RowingClub.Identity.Domain.Users.User).Assembly },
        { "Clubs.Domain", typeof(RowingClub.Clubs.Domain.AssemblyMarker).Assembly },
        { "Memberships.Domain", typeof(RowingClub.Memberships.Domain.AssemblyMarker).Assembly },
        { "Scheduling.Domain", typeof(RowingClub.Scheduling.Domain.AssemblyMarker).Assembly },
        { "Packages.Domain", typeof(RowingClub.Packages.Domain.AssemblyMarker).Assembly },
        { "Notifications.Domain", typeof(RowingClub.Notifications.Domain.AssemblyMarker).Assembly },
    };

    public static TheoryData<string, Assembly> ApplicationAssemblies { get; } = new()
    {
        { "BuildingBlocks.Application", typeof(RowingClub.BuildingBlocks.Application.DependencyInjection).Assembly },
        { "Identity.Application", typeof(RowingClub.Identity.Application.DependencyInjection).Assembly },
    };

    [Theory]
    [MemberData(nameof(DomainAssemblies))]
    public void Domain_should_not_depend_on_infrastructure_or_application(string _, Assembly assembly)
    {
        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny("Infrastructure", "Microsoft.EntityFrameworkCore", "MediatR", "FluentValidation")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatFailures(result));
    }

    [Theory]
    [MemberData(nameof(ApplicationAssemblies))]
    public void Application_should_not_depend_on_infrastructure(string _, Assembly assembly)
    {
        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny("Npgsql", "Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatFailures(result));
    }

    [Fact]
    public void Identity_infrastructure_should_not_depend_on_other_modules()
    {
        var assembly = typeof(RowingClub.Identity.Infrastructure.Repositories.UserRepository).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "RowingClub.Clubs", "RowingClub.Memberships", "RowingClub.Scheduling",
                "RowingClub.Packages", "RowingClub.Notifications", "RowingClub.Reporting")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatFailures(result));
    }

    [Fact]
    public void Api_layer_types_should_not_be_referenced_from_any_module_infrastructure()
    {
        var assembly = typeof(RowingClub.Identity.Infrastructure.Repositories.UserRepository).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny("RowingClub.Api", "RowingClub.Bootstrapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatFailures(result));
    }

    private static string FormatFailures(TestResult result) =>
        result.FailingTypes is null
            ? "bilinmeyen ihlal"
            : string.Join(", ", result.FailingTypes.Select(t => t.FullName));
}
