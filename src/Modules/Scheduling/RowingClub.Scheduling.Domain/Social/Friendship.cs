using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Scheduling.Domain.Social;

public enum FriendshipStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
}

/// <summary>
/// Üyeler arası arkadaşlık. İstek, hedef üyenin benzersiz üye koduyla gönderilir; kabul edilince
/// iki üye birbirine mesaj atabilir. Tenant veritabanında yaşar - arkadaşlık yalnızca aynı kulüp
/// üyeleri arasında kurulabilir.
/// </summary>
public sealed class Friendship
{
    public Guid Id { get; private set; }

    /// <summary>İsteği gönderen üye.</summary>
    public Guid RequesterId { get; private set; }

    /// <summary>İsteği alan üye.</summary>
    public Guid AddresseeId { get; private set; }

    public FriendshipStatus Status { get; private set; }

    public DateTimeOffset RequestedAtUtc { get; private set; }

    public DateTimeOffset? RespondedAtUtc { get; private set; }

    private Friendship()
    {
    }

    public static Friendship Request(Guid requesterId, Guid addresseeId)
    {
        if (requesterId == addresseeId)
            throw new DomainException("cannot_friend_self", "Kendinizi arkadaş olarak ekleyemezsiniz.");

        return new Friendship
        {
            Id = Guid.NewGuid(),
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = FriendshipStatus.Pending,
            RequestedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Respond(bool accept)
    {
        if (Status != FriendshipStatus.Pending)
            throw new DomainException("already_responded", "Bu istek zaten yanıtlanmış.");

        Status = accept ? FriendshipStatus.Accepted : FriendshipStatus.Rejected;
        RespondedAtUtc = DateTimeOffset.UtcNow;
    }

    public bool Involves(Guid customerId) => RequesterId == customerId || AddresseeId == customerId;

    public Guid OtherThan(Guid customerId) => RequesterId == customerId ? AddresseeId : RequesterId;
}
