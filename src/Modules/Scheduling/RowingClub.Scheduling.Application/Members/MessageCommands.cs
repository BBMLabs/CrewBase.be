using FluentValidation;
using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Social;

namespace RowingClub.Scheduling.Application.Members;

public sealed record MessageDto(
    Guid Id, Guid SenderId, Guid RecipientId, string Body, DateTimeOffset SentAtUtc, bool Read);

/// <summary>Gerçek zamanlı iletim: API katmanı SignalR hub'ı üzerinden alıcıya iletir.</summary>
public interface IChatNotifier
{
    Task PushMessageAsync(Guid recipientCustomerId, MessageDto message, CancellationToken cancellationToken);
}

public sealed record SendDirectMessageCommand(Guid SenderId, Guid RecipientId, string Body)
    : ICommand<MessageDto>;

public sealed class SendDirectMessageCommandValidator : AbstractValidator<SendDirectMessageCommand>
{
    public SendDirectMessageCommandValidator()
    {
        RuleFor(c => c.Body).NotEmpty().MaximumLength(DirectMessage.MaxLength);
    }
}

public sealed record GetConversationQuery(Guid CustomerId, Guid FriendCustomerId, int Take)
    : ICommand<List<MessageDto>>;

public sealed class SendDirectMessageCommandHandler(
    IFriendshipRepository friendshipRepository,
    IDirectMessageRepository messageRepository,
    IChatNotifier chatNotifier,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<SendDirectMessageCommand, MessageDto>
{
    public async Task<MessageDto> Handle(SendDirectMessageCommand request, CancellationToken cancellationToken)
    {
        var body = request.Body?.Trim() ?? "";
        if (body.Length == 0)
            throw new DomainException("empty_message", "Boş mesaj gönderilemez.");
        if (body.Length > DirectMessage.MaxLength)
            throw new DomainException("message_too_long", $"Mesaj en fazla {DirectMessage.MaxLength} karakter olabilir.");

        // Mesajlaşma yalnızca kabul edilmiş arkadaşlar arasında.
        if (!await friendshipRepository.AreFriendsAsync(request.SenderId, request.RecipientId, cancellationToken))
            throw new DomainException("not_friends", "Yalnızca arkadaşlarınıza mesaj gönderebilirsiniz.");

        var message = DirectMessage.Send(request.SenderId, request.RecipientId, body);
        messageRepository.Add(message);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new MessageDto(
            message.Id, message.SenderId, message.RecipientId, message.Body, message.SentAtUtc, false);

        // Push başarısız olsa bile mesaj kalıcıdır; alıcı geçmişten görür.
        try
        {
            await chatNotifier.PushMessageAsync(request.RecipientId, dto, cancellationToken);
        }
        catch
        {
            // realtime iletim best-effort
        }

        return dto;
    }
}

public sealed class GetConversationQueryHandler(
    IFriendshipRepository friendshipRepository,
    IDirectMessageRepository messageRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<GetConversationQuery, List<MessageDto>>
{
    public async Task<List<MessageDto>> Handle(GetConversationQuery request, CancellationToken cancellationToken)
    {
        if (!await friendshipRepository.AreFriendsAsync(
                request.CustomerId, request.FriendCustomerId, cancellationToken))
            throw new DomainException("not_friends", "Bu üyeyle arkadaş değilsiniz.");

        var messages = await messageRepository.GetConversationAsync(
            request.CustomerId, request.FriendCustomerId, Math.Clamp(request.Take, 1, 200), cancellationToken);

        // Bana gelenleri okundu işaretle.
        foreach (var m in messages.Where(m => m.RecipientId == request.CustomerId && m.ReadAtUtc is null))
            m.MarkRead();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return messages
            .OrderBy(m => m.SentAtUtc)
            .Select(m => new MessageDto(m.Id, m.SenderId, m.RecipientId, m.Body, m.SentAtUtc, m.ReadAtUtc is not null))
            .ToList();
    }
}
