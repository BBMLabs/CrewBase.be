using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.BuildingBlocks.Application.Behaviors;

public sealed class PlatformActivityLogBehavior<TRequest, TResponse>(
    IServiceProvider serviceProvider,
    ICurrentUser currentUser,
    ICurrentRequestContext currentRequestContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is ICommand<TResponse> && currentUser.IsAuthenticated && currentUser.IsPlatformAdmin)
        {
            var action = typeof(TRequest).Name;
            if (action.EndsWith("Command", StringComparison.Ordinal))
                action = action[..^"Command".Length];

            var targetCompanyId = typeof(TRequest).GetProperty("CompanyId")?.GetValue(request) as Guid?;

            var writer = serviceProvider.GetRequiredService<IPlatformActivityLogWriter>();
            await writer.RecordAsync(
                action, targetCompanyId, currentRequestContext.IpAddress, currentRequestContext.UserAgent, cancellationToken);
        }

        return response;
    }
}
