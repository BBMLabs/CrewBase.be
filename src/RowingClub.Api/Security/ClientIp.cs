using System.Net;
using Microsoft.AspNetCore.Http;

namespace RowingClub.Api.Security;

public static class ClientIp
{
    public static string Resolve(HttpContext httpContext)
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var ip = forwardedFor.Split(',')[0].Trim();
            if (IPAddress.TryParse(ip, out _))
                return ip;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
