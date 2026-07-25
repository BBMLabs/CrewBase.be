using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;
using RowingClub.Identity.Infrastructure.Repositories;

namespace RowingClub.IntegrationTests.Identity;

[Collection(nameof(PostgresIdentityCollection))]
public sealed class UserRepositoryTests(PostgresIdentityFixture fixture)
{
    [Fact]
    public async Task Add_then_SaveChanges_persists_the_user_and_drains_its_domain_event_to_the_outbox()
    {
        var repository = new UserRepository(fixture.Context);
        var email = EmailAddress.Create($"{Guid.NewGuid()}@example.com");
        var user = User.Register(email);

        repository.Add(user);
        await fixture.UnitOfWork.SaveChangesAsync(CancellationToken.None);

        var reloaded = await repository.GetByEmailAsync(email, CancellationToken.None);
        reloaded.Should().NotBeNull();
        reloaded!.Id.Should().Be(user.Id);

        var outboxCount = await fixture.Context.OutboxMessages
            .CountAsync(m => m.Content.Contains(user.Id.ToString()));
        outboxCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Mutating_a_loaded_user_and_saving_persists_the_mutation()
    {
        var repository = new UserRepository(fixture.Context);
        var email = EmailAddress.Create($"{Guid.NewGuid()}@example.com");
        var user = User.Register(email);
        repository.Add(user);
        await fixture.UnitOfWork.SaveChangesAsync(CancellationToken.None);

        var loaded = await repository.GetByEmailAsync(email, CancellationToken.None);
        loaded!.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));
        await fixture.UnitOfWork.SaveChangesAsync(CancellationToken.None);

        fixture.Context.ChangeTracker.Clear();
        var reloaded = await repository.GetByEmailAsync(email, CancellationToken.None);
        reloaded!.FailedLoginAttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task Concurrent_writers_racing_on_the_same_user_throws_ConcurrencyException_for_the_loser()
    {
        var repository = new UserRepository(fixture.Context);
        var email = EmailAddress.Create($"{Guid.NewGuid()}@example.com");
        var user = User.Register(email);
        repository.Add(user);
        await fixture.UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Two independent DbContext instances load the SAME row and both try to save - mimics two
        // concurrent web requests.
        var connectionString = fixture.Context.Database.GetConnectionString()!;
        var contextOptions = new DbContextOptionsBuilder<RowingClubDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var firstContext = new RowingClubDbContext(
            contextOptions, NoopTenant.Instance, NoopCorrelation.Instance,
            [new PersistenceAssemblyMarker(typeof(RowingClub.Identity.Infrastructure.DependencyInjection).Assembly)]);
        await using var secondContext = new RowingClubDbContext(
            contextOptions, NoopTenant.Instance, NoopCorrelation.Instance,
            [new PersistenceAssemblyMarker(typeof(RowingClub.Identity.Infrastructure.DependencyInjection).Assembly)]);

        var firstRepo = new UserRepository(firstContext);
        var secondRepo = new UserRepository(secondContext);

        var firstCopy = await firstRepo.GetByEmailAsync(email, CancellationToken.None);
        var secondCopy = await secondRepo.GetByEmailAsync(email, CancellationToken.None);

        firstCopy!.RegisterSuccessfulLogin();
        secondCopy!.RegisterSuccessfulLogin();

        await firstContext.SaveChangesAsync(CancellationToken.None);

        var act = () => secondContext.SaveChangesAsync(CancellationToken.None);
        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    private sealed class NoopTenant : RowingClub.BuildingBlocks.Application.Abstractions.ICurrentTenant
    {
        public static readonly NoopTenant Instance = new();
        public bool IsSet => false;
        public Guid ClubId => throw new InvalidOperationException();
    }

    private sealed class NoopCorrelation : RowingClub.BuildingBlocks.Application.Abstractions.ICorrelationIdAccessor
    {
        public static readonly NoopCorrelation Instance = new();
        public string CorrelationId => "integration-test";
        public void Set(string correlationId)
        {
        }
    }
}
