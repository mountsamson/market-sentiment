using System.Diagnostics;

namespace MarketSentiment.Middleware;

/// <summary>
/// Propagates or generates a correlation ID for distributed request tracing.
/// Reads X-Correlation-Id from the inbound request (so upstream callers can
/// inject their own ID), or creates a new one if absent.
/// Stamps it on the outbound response header and the structured log scope
/// so every log line for a request shares the same ID.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N")[..16];

        context.Items[HeaderName] = correlationId;
        context.Response.Headers.Append(HeaderName, correlationId);

        var sw = Stopwatch.StartNew();

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"]  = correlationId,
            ["RequestMethod"]  = context.Request.Method,
            ["RequestPath"]    = context.Request.Path.Value ?? string.Empty,
        }))
        {
            try
            {
                await next(context);
            }
            finally
            {
                sw.Stop();
                logger.LogInformation(
                    "HTTP {Method} {Path} → {StatusCode} in {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    sw.ElapsedMilliseconds);
            }
        }
    }
}
