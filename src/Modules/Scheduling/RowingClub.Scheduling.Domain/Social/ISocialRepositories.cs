namespace RowingClub.Scheduling.Domain.Social;

public interface IFriendshipRepository
{
    Task<Friendship?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>İki üye arasındaki (yönden bağımsız) mevcut ilişki.</summary>
    Task<Friendship?> GetBetweenAsync(Guid a, Guid b, CancellationToken cancellationToken);

    Task<List<Friendship>> GetForCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    Task<bool> AreFriendsAsync(Guid a, Guid b, CancellationToken cancellationToken);

    void Add(Friendship friendship);

    void Remove(Friendship friendship);
}

public interface IDirectMessageRepository
{
    Task<List<DirectMessage>> GetConversationAsync(
        Guid a, Guid b, int take, CancellationToken cancellationToken);

    /// <summary>Arkadaş listesi için: kişi başına okunmamış mesaj sayıları.</summary>
    Task<Dictionary<Guid, int>> GetUnreadCountsAsync(Guid recipientId, CancellationToken cancellationToken);

    void Add(DirectMessage message);
}
