using System.Diagnostics;
using Forge.Application.Abstractions;

namespace Forge.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context, ICurrentUser currentUser)
    {
        var stopwatch = Stopwatch.StartNew();
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            using var scope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["TraceId"] = traceId,
                ["UserId"] = currentUser.UserId
            });
            _logger.LogInformation(
                "request.handled {Method} {Path} -> {Status} in {DurationMs}ms TraceId={TraceId} UserId={UserId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                traceId,
                currentUser.UserId);
        }
    }
}
