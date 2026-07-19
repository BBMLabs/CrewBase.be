using RowingClub.BuildingBlocks.Application.Abstractions;
using StackExchange.Redis;

namespace RowingClub.BuildingBlocks.Infrastructure.Idempotency;

public sealed class RedisIdempotencyStore(IConnectionMultiplexer connectionMultiplexer) : IIdempotencyStore
{
    private IDatabase Database => connectionMultiplexer.GetDatabase();

    public async Task<(bool Found, string? SerializedResponse)> TryGetAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var value = await Database.StringGetAsync(key);
        return value.HasValue ? (true, (string)value!) : (false, null);
    }

    public Task StoreAsync(string key, string serializedResponse, TimeSpan ttl, CancellationToken cancellationToken) =>
        Database.StringSetAsync(key, serializedResponse, ttl, When.NotExists);
}
