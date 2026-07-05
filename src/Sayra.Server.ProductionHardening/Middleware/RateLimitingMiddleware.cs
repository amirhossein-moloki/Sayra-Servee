using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Net;

namespace Sayra.Server.ProductionHardening.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, RequestLog> _requestTracker = new();
    private const int MaxRequestsPerMinute = 60;

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;

        var log = _requestTracker.AddOrUpdate(ipAddress,
            _ => new RequestLog(now, 1),
            (_, existing) =>
            {
                if (now - existing.WindowStart > TimeSpan.FromMinutes(1))
                {
                    return new RequestLog(now, 1);
                }
                return existing with { Count = existing.Count + 1 };
            });

        if (log.Count > MaxRequestsPerMinute)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            await context.Response.WriteAsync("Rate limit exceeded. Try again in a minute.");
            return;
        }

        await _next(context);
    }

    private record RequestLog(DateTime WindowStart, int Count);
}
