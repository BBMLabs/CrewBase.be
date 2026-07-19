using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Identity.Domain.Tokens;

namespace RowingClub.Identity.Application.LogoutAll;

public sealed class LogoutAllCommandHandler(
    ICurrentUser currentUser,
    IUserSessionRepository userSessionRepository)
    : IRequestHandler<LogoutAllCommand, Unit>
{
    public async Task<Unit> Handle(LogoutAllCommand request, CancellationToken cancellationToken)
    {
        var activeSessions = await userSessionRepository.GetActiveByUserIdAsync(
            currentUser.UserId, cancellationToken);

        foreach (var session in activeSessions)
        {
            session.Revoke();
        }

        return Unit.Value;
    }
}
