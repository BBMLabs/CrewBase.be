using RowingClub.BuildingBlocks.Application.Abstractions;

namespace RowingClub.Api.Security;

public sealed class HttpContextCurrentRequestContext(IHttpContextAccessor httpContextAccessor) : ICurrentRequestContext
{
    public string? IpAddress => httpContextAccessor.HttpContext is { } context ? ClientIp.Resolve(context) : null;

    public string? UserAgent
    {
        get
        {
            var userAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
            return string.IsNullOrWhiteSpace(userAgent) ? null : userAgent;
        }
    }
}
