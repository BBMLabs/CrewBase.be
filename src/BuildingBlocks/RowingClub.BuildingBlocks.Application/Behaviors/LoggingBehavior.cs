using MediatR;
using Microsoft.Extensions.Logging;
using RowingClub.BuildingBlocks.Application.Abstractions;

namespace RowingClub.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Structured start/end log per request (spec section 19 - CorrelationId, UserId, ClubId,
/// Duration, Event name are mandatory log fields). Never logs request/response payloads, since
/// commands may carry sensitive fields (spec section 9).
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUser currentUser,
    ICurrentTenant currentTenant)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = currentUser.IsAuthenticated ? currentUser.UserId : (Guid?)null;
        var clubId = currentTenant.IsSet ? currentTenant.ClubId : (Guid?)null;

        logger.LogInformation(
            "İşlem başladı: {RequestName} UserId={UserId} ClubId={ClubId}",
            requestName, userId, clubId);

        try
        {
            var response = await next();

            logger.LogInformation("İşlem tamamlandı: {RequestName}", requestName);

            return response;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "İşlem başarısız: {RequestName} UserId={UserId} ClubId={ClubId}",
                requestName, userId, clubId);
            throw;
        }
    }
}
