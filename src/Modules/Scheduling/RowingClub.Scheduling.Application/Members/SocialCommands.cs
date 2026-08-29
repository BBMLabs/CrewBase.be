using FluentValidation;
using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Social;

namespace RowingClub.Scheduling.Application.Members;

public sealed record FriendDto(
    Guid FriendshipId,
    Guid CustomerId,
    string FullName,
    string? MemberCode,
    int Level,
    string Direction,   // "incoming" | "outgoing" | "friend"
    string Status,
    int UnreadCount);

/// <summary>Üyenin kendi benzersiz kodu (yoksa atanır) - arkadaşlar bu kodla ekler.</summary>
public sealed record GetMyCodeQuery(Guid CustomerId) : ICommand<string>;

public sealed record SendFriendRequestCommand(Guid CustomerId, string MemberCode) : ICommand<FriendDto>;

public sealed class SendFriendRequestCommandValidator : AbstractValidator<SendFriendRequestCommand>
{
    public SendFriendRequestCommandValidator()
    {
        RuleFor(c => c.MemberCode).NotEmpty().MaximumLength(20);
    }
}

public sealed record RespondFriendRequestCommand(Guid CustomerId, Guid FriendshipId, bool Accept) : ICommand<Unit>;

public sealed record GetFriendsQuery(Guid CustomerId) : IRequest<List<FriendDto>>;

/// <summary>Kod ataması: çakışırsa yeniden üretir (unique index son güvence).</summary>
public static class MemberCodeAssigner
{
    public static async Task<string> EnsureCodeAsync(
        Customer customer, ICustomerRepository repository, CancellationToken cancellationToken)
    {
        if (customer.MemberCode is { } existing)
            return existing;

        for (var i = 0; i < 10; i++)
        {
            var code = MemberCodeGenerator.Generate();
            if (!await repository.ExistsByMemberCodeAsync(code, cancellationToken))
            {
                customer.AssignMemberCode(code);
                return code;
            }
        }

        throw new DomainException("code_generation_failed", "Üye kodu üretilemedi; tekrar deneyin.");
    }
}

public sealed class GetMyCodeQueryHandler(
    ICustomerRepository customerRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<GetMyCodeQuery, string>
{
    public async Task<string> Handle(GetMyCodeQuery request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId.ToString());

        var code = await MemberCodeAssigner.EnsureCodeAsync(customer, customerRepository, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return code;
    }
}

public sealed class SendFriendRequestCommandHandler(
    ICustomerRepository customerRepository,
    IFriendshipRepository friendshipRepository,
    IMemberLogRepository memberLogRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<SendFriendRequestCommand, FriendDto>
{
    public async Task<FriendDto> Handle(SendFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var target = await customerRepository.GetByMemberCodeAsync(
            MemberCodeGenerator.Normalize(request.MemberCode), cancellationToken)
            ?? throw new DomainException("code_not_found", "Bu koda sahip bir üye bulunamadı.");

        if (target.Id == request.CustomerId)
            throw new DomainException("cannot_friend_self", "Kendinizi arkadaş olarak ekleyemezsiniz.");

        var existing = await friendshipRepository.GetBetweenAsync(request.CustomerId, target.Id, cancellationToken);
        if (existing is not null)
        {
            if (existing.Status == FriendshipStatus.Accepted)
                throw new DomainException("already_friends", "Bu üyeyle zaten arkadaşsınız.");
            if (existing.Status == FriendshipStatus.Pending)
                throw new DomainException("request_pending", "Bekleyen bir arkadaşlık isteği zaten var.");

            // Reddedilmiş eski kayıt: temizle, yeniden istenebilsin.
            friendshipRepository.Remove(existing);
        }

        var friendship = Friendship.Request(request.CustomerId, target.Id);
        friendshipRepository.Add(friendship);
        memberLogRepository.Add(MemberLog.Record(request.CustomerId, "FRIEND_REQUEST_SENT", target.FullName));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new FriendDto(
            friendship.Id, target.Id, target.FullName, target.MemberCode, target.Level,
            "outgoing", friendship.Status.ToString(), 0);
    }
}

public sealed class RespondFriendRequestCommandHandler(
    IFriendshipRepository friendshipRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<RespondFriendRequestCommand, Unit>
{
    public async Task<Unit> Handle(RespondFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var friendship = await friendshipRepository.GetByIdAsync(request.FriendshipId, cancellationToken)
            ?? throw new NotFoundException("Friendship", request.FriendshipId.ToString());

        // Yalnızca isteğin MUHATABI yanıtlayabilir (IDOR koruması).
        if (friendship.AddresseeId != request.CustomerId)
            throw new DomainException("forbidden", "Bu isteği yalnızca alan kişi yanıtlayabilir.");

        friendship.Respond(request.Accept);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public sealed class GetFriendsQueryHandler(
    IFriendshipRepository friendshipRepository,
    ICustomerRepository customerRepository,
    IDirectMessageRepository messageRepository)
    : IRequestHandler<GetFriendsQuery, List<FriendDto>>
{
    public async Task<List<FriendDto>> Handle(GetFriendsQuery request, CancellationToken cancellationToken)
    {
        var friendships = await friendshipRepository.GetForCustomerAsync(request.CustomerId, cancellationToken);
        var unread = await messageRepository.GetUnreadCountsAsync(request.CustomerId, cancellationToken);

        var result = new List<FriendDto>();
        foreach (var friendship in friendships.Where(f => f.Status != FriendshipStatus.Rejected))
        {
            var otherId = friendship.OtherThan(request.CustomerId);
            var other = await customerRepository.GetByIdAsync(otherId, cancellationToken);
            if (other is null)
                continue;

            var direction = friendship.Status == FriendshipStatus.Accepted
                ? "friend"
                : friendship.RequesterId == request.CustomerId ? "outgoing" : "incoming";

            result.Add(new FriendDto(
                friendship.Id, other.Id, other.FullName, other.MemberCode, other.Level,
                direction, friendship.Status.ToString(), unread.GetValueOrDefault(otherId)));
        }

        return result
            .OrderByDescending(f => f.Direction == "incoming")
            .ThenBy(f => f.FullName)
            .ToList();
    }
}
