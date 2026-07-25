using Microsoft.AspNetCore.Http;

namespace RowingClub.SecurityTests;

public sealed class FakeHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; }

    public FakeHttpContextAccessor(HttpContext httpContext)
    {
        HttpContext = httpContext;
    }
}
