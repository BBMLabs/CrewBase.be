using FluentValidation;
using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Messages;

namespace RowingClub.Scheduling.Application.Messages;

public sealed record SiteMessageDto(
    Guid Id, string FullName, string Email, string Body, DateTimeOffset CreatedAtUtc,
    string? ReplyText, DateTimeOffset? RepliedAtUtc);

public sealed record SubmitSiteMessageCommand(string FullName, string Email, string Body, string? IpAddress)
    : ICommand<Unit>;

public sealed record GetSiteMessagesQuery(string? Cursor = null, int Limit = 25) : IRequest<KeysetResult<SiteMessageDto>>;

public sealed record ReplySiteMessageCommand(Guid MessageId, string ReplyText, string CompanyName)
    : ICommand<SiteMessageDto>;

public sealed class SubmitSiteMessageCommandValidator : AbstractValidator<SubmitSiteMessageCommand>
{
    public SubmitSiteMessageCommandValidator()
    {
        RuleFor(c => c.FullName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(c => c.Body).NotEmpty().MaximumLength(4000);
    }
}

public sealed class SubmitSiteMessageCommandHandler(
    ISiteMessageRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<SubmitSiteMessageCommand, Unit>
{
    public async Task<Unit> Handle(SubmitSiteMessageCommand request, CancellationToken cancellationToken)
    {
        repository.Add(SiteMessage.Create(request.FullName, request.Email, request.Body, request.IpAddress));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public sealed class GetSiteMessagesQueryHandler(ISiteMessageRepository repository)
    : IRequestHandler<GetSiteMessagesQuery, KeysetResult<SiteMessageDto>>
{
    public async Task<KeysetResult<SiteMessageDto>> Handle(GetSiteMessagesQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 200);
        var hasCursor = KeysetCursor.TryDecode(request.Cursor, 1, out var keyParts, out var cursorId);
        var cursorAtUtc = hasCursor
            ? DateTimeOffset.Parse(keyParts[0], System.Globalization.CultureInfo.InvariantCulture)
            : (DateTimeOffset?)null;

        var messages = await repository.GetPageAsync(
            cursorAtUtc, hasCursor ? cursorId : null, limit + 1, cancellationToken);

        var (page, nextCursor) = KeysetPage.Trim(
            messages, limit, m => m.Id,
            m => [m.CreatedAtUtc.ToString("o", System.Globalization.CultureInfo.InvariantCulture)]);

        return new KeysetResult<SiteMessageDto>(page.Select(ToDto).ToList(), nextCursor);
    }

    internal static SiteMessageDto ToDto(SiteMessage m) =>
        new(m.Id, m.FullName, m.Email, m.Body, m.CreatedAtUtc, m.ReplyText, m.RepliedAtUtc);
}

public interface ISiteMessageReplySender
{
    Task SendAsync(
        string email, string fullName, string companyName, string originalMessage, string replyText,
        CancellationToken cancellationToken);
}

public sealed class ReplySiteMessageCommandHandler(
    ISiteMessageRepository repository, ISiteMessageReplySender replySender, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<ReplySiteMessageCommand, SiteMessageDto>
{
    public async Task<SiteMessageDto> Handle(ReplySiteMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await repository.GetByIdAsync(request.MessageId, cancellationToken)
            ?? throw new NotFoundException("SiteMessage", request.MessageId.ToString());

        message.Reply(request.ReplyText);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await replySender.SendAsync(
            message.Email, message.FullName, request.CompanyName, message.Body, request.ReplyText, cancellationToken);

        return GetSiteMessagesQueryHandler.ToDto(message);
    }
}
