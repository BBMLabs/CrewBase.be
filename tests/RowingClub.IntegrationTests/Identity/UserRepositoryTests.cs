using FluentAssertions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;
using RowingClub.Identity.Infrastructure.Repositories;

namespace RowingClub.IntegrationTests.Identity;

[Collection(nameof(MongoIdentityCollection))]
public sealed class UserRepositoryTests(MongoIdentityFixture fixture)
{
    [Fact]
    public async Task Add_then_SaveChanges_persists_the_user_and_drains_its_domain_event_to_the_outbox()
    {
        var repository = new MongoUserRepository(fixture.Database, fixture.UnitOfWork);
        var email = EmailAddress.Create($"{Guid.NewGuid()}@example.com");
        var user = User.Register(email);

        repository.Add(user);
        await fixture.UnitOfWork.SaveChangesAsync(CancellationToken.None);

        var reloaded = await repository.GetByEmailAsync(email, CancellationToken.None);
        reloaded.Should().NotBeNull();
        reloaded!.Id.Should().Be(user.Id);

        var outboxFilter = new MongoDB.Bson.BsonDocument(
            "content", new MongoDB.Bson.BsonDocument("$regex", user.Id.ToString()));
        var outboxCount = await fixture.Database
            .GetCollection<MongoDB.Bson.BsonDocument>("outbox_messages")
            .CountDocumentsAsync(outboxFilter);
        outboxCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Mutating_a_loaded_user_and_saving_persists_the_mutation()
    {
        var repository = new MongoUserRepository(fixture.Database, fixture.UnitOfWork);
        var email = EmailAddress.Create($"{Guid.NewGuid()}@example.com");
        var user = User.Register(email);
        repository.Add(user);
        await fixture.UnitOfWork.SaveChangesAsync(CancellationToken.None);

        var loaded = await repository.GetByEmailAsync(email, CancellationToken.None);
        loaded!.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));
        await fixture.UnitOfWork.SaveChangesAsync(CancellationToken.None);

        var reloaded = await repository.GetByEmailAsync(email, CancellationToken.None);
        reloaded!.FailedLoginAttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task Concurrent_writers_racing_on_the_same_user_throws_ConcurrencyException_for_the_loser()
    {
        var repository = new MongoUserRepository(fixture.Database, fixture.UnitOfWork);
        var email = EmailAddress.Create($"{Guid.NewGuid()}@example.com");
        var user = User.Register(email);
        repository.Add(user);
        await fixture.UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Two independent unit-of-work instances load the SAME document version and both try to save.
        var firstUow = new RowingClub.BuildingBlocks.Infrastructure.Mongo.MongoUnitOfWork(
            fixture.Database, NoopTenant.Instance, NoopCorrelation.Instance);
        var secondUow = new RowingClub.BuildingBlocks.Infrastructure.Mongo.MongoUnitOfWork(
            fixture.Database, NoopTenant.Instance, NoopCorrelation.Instance);

        var firstRepo = new MongoUserRepository(fixture.Database, firstUow);
        var secondRepo = new MongoUserRepository(fixture.Database, secondUow);

        var firstCopy = await firstRepo.GetByEmailAsync(email, CancellationToken.None);
        var secondCopy = await secondRepo.GetByEmailAsync(email, CancellationToken.None);

        firstCopy!.RegisterSuccessfulLogin();
        secondCopy!.RegisterSuccessfulLogin();

        await firstUow.SaveChangesAsync(CancellationToken.None);

        var act = () => secondUow.SaveChangesAsync(CancellationToken.None);
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
