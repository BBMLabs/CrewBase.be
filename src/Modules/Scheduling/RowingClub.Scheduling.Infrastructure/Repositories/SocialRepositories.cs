using Microsoft.EntityFrameworkCore;
using RowingClub.Scheduling.Domain.Cards;
using RowingClub.Scheduling.Domain.Consents;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Social;
using RowingClub.Scheduling.Infrastructure.Persistence;

namespace RowingClub.Scheduling.Infrastructure.Repositories;

public sealed class FriendshipRepository(TenantDbContext context) : IFriendshipRepository
{
    public Task<Friendship?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Friendships.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public Task<Friendship?> GetBetweenAsync(Guid a, Guid b, CancellationToken cancellationToken) =>
        context.Friendships.FirstOrDefaultAsync(
            f => (f.RequesterId == a && f.AddresseeId == b) || (f.RequesterId == b && f.AddresseeId == a),
            cancellationToken);

    public Task<List<Friendship>> GetForCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        context.Friendships
            .Where(f => f.RequesterId == customerId || f.AddresseeId == customerId)
            .ToListAsync(cancellationToken);

    public Task<bool> AreFriendsAsync(Guid a, Guid b, CancellationToken cancellationToken) =>
        context.Friendships.AnyAsync(
            f => f.Status == FriendshipStatus.Accepted &&
                 ((f.RequesterId == a && f.AddresseeId == b) || (f.RequesterId == b && f.AddresseeId == a)),
            cancellationToken);

    public void Add(Friendship friendship) => context.Friendships.Add(friendship);

    public void Remove(Friendship friendship) => context.Friendships.Remove(friendship);
}

public sealed class DirectMessageRepository(TenantDbContext context) : IDirectMessageRepository
{
    public async Task<List<DirectMessage>> GetConversationAsync(
        Guid a, Guid b, int take, CancellationToken cancellationToken) =>
        await context.DirectMessages
            .Where(m => (m.SenderId == a && m.RecipientId == b) || (m.SenderId == b && m.RecipientId == a))
            .OrderByDescending(m => m.SentAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<Guid, int>> GetUnreadCountsAsync(
        Guid recipientId, CancellationToken cancellationToken)
    {
        var groups = await context.DirectMessages
            .Where(m => m.RecipientId == recipientId && m.ReadAtUtc == null)
            .GroupBy(m => m.SenderId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return groups.ToDictionary(g => g.Key, g => g.Count);
    }

    public void Add(DirectMessage message) => context.DirectMessages.Add(message);
}

public sealed class ConsentRecordRepository(TenantDbContext context) : IConsentRecordRepository
{
    public async Task<Dictionary<string, ConsentRecord>> GetLatestByCustomerAsync(
        Guid customerId, CancellationToken cancellationToken)
    {
        var records = await context.ConsentRecords
            .Where(c => c.CustomerId == customerId)
            .OrderBy(c => c.AcceptedAtUtc)
            .ToListAsync(cancellationToken);

        // Aynı anahtarda en güncel kayıt kazanır (misafirler tekrar tekrar onaylar).
        return records
            .GroupBy(c => c.ConsentKey)
            .ToDictionary(g => g.Key, g => g.Last());
    }

    public void Add(ConsentRecord record) => context.ConsentRecords.Add(record);
}

public sealed class MembershipCardRepository(TenantDbContext context) : IMembershipCardRepository
{
    public Task<List<MembershipCard>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        context.MembershipCards.Where(c => c.CustomerId == customerId).ToListAsync(cancellationToken);

    public Task<MembershipCard?> GetAsync(
        Guid customerId, MembershipCardType type, CancellationToken cancellationToken) =>
        context.MembershipCards.FirstOrDefaultAsync(
            c => c.CustomerId == customerId && c.Type == type, cancellationToken);

    public void Add(MembershipCard card) => context.MembershipCards.Add(card);
}

public sealed class VerificationCodeRepository(TenantDbContext context) : IVerificationCodeRepository
{
    public async Task<VerificationCode?> GetActiveAsync(
        Guid customerId, VerificationPurpose purpose, CancellationToken cancellationToken)
    {
        var codes = await context.VerificationCodes
            .Where(v => v.CustomerId == customerId && v.Purpose == purpose && v.UsedAtUtc == null)
            .OrderByDescending(v => v.ExpiresAtUtc)
            .ToListAsync(cancellationToken);

        return codes.FirstOrDefault();
    }

    public void Add(VerificationCode code) => context.VerificationCodes.Add(code);

    public void Remove(VerificationCode code) => context.VerificationCodes.Remove(code);
}
