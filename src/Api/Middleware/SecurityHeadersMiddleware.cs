namespace LocationManagement.Api.Middleware;

/// <summary>
/// Middleware that appends security-related HTTP response headers to every response.
/// Sets X-Content-Type-Options, X-Frame-Options, and Referrer-Policy as required by Requirement 15.7.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Initialises the middleware with the next delegate in the pipeline.</summary>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>Adds security headers then invokes the next middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";

        await _next(context);
    }
}
