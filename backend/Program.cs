using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MarketSentiment.Middleware;
using MarketSentiment.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddMemoryCache();

const string userAgent = "Mozilla/5.0 (compatible; MarketSentimentBot/1.0)";

builder.Services.AddHttpClient<MarketDataService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", userAgent);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<NewsService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", userAgent);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// ── Health Checks ─────────────────────────────────────────────────────────────
// Exposes GET /health for Docker, Kubernetes liveness/readiness probes,
// and monitoring systems (CloudWatch, Grafana, Prometheus scrape target).

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Service is running"), tags: ["live"])
    .AddAsyncCheck("yahoo-finance", async () =>
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            http.DefaultRequestHeaders.Add("User-Agent", userAgent);
            var res = await http.GetAsync("https://query1.finance.yahoo.com");
            return res.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Yahoo Finance reachable")
                : HealthCheckResult.Degraded($"Yahoo Finance returned {(int)res.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cannot reach Yahoo Finance", ex);
        }
    }, tags: ["ready"]);

// ── Rate Limiting ─────────────────────────────────────────────────────────────

builder.Services.AddRateLimiter(options =>
{
    // REST endpoints: 60 requests per minute per IP.
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit = 60;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });

    // SSE stream: max 5 concurrent connections per IP (streams are long-lived).
    options.AddConcurrencyLimiter("stream", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.Headers.Append("Retry-After", "60");
        ctx.HttpContext.Response.ContentType = "application/json";
        await ctx.HttpContext.Response.WriteAsync(
            "{\"error\":\"Rate limit exceeded. Please wait before retrying.\"}", token);
    };
});

// ── CORS ──────────────────────────────────────────────────────────────────────

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .WithMethods("GET")
            .AllowCredentials();
    });
});

// ── Pipeline ──────────────────────────────────────────────────────────────────

var app = builder.Build();

// Global exception handler — never expose stack traces to clients.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\":\"An internal error occurred.\"}");
    });
});

// Security headers on every response.
app.Use(async (context, next) =>
{
    var h = context.Response.Headers;
    h.Append("X-Content-Type-Options",  "nosniff");
    h.Append("X-Frame-Options",         "DENY");
    h.Append("X-XSS-Protection",        "1; mode=block");
    h.Append("Referrer-Policy",          "strict-origin-when-cross-origin");
    h.Append("Content-Security-Policy", "default-src 'none'");
    h.Append("Permissions-Policy",      "geolocation=(), microphone=(), camera=()");
    await next();
});

// Correlation ID + structured request timing (must run before routing).
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseRateLimiter();
app.UseCors("FrontendPolicy");
app.UseAuthorization();
app.MapControllers();

// Health endpoints — exempt from rate limiting and auth.
// /health/live  → liveness  (is the process alive?)
// /health/ready → readiness (can it serve traffic? Yahoo Finance reachable?)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = WriteHealthJson
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") || check.Tags.Contains("live"),
    ResponseWriter = WriteHealthJson
});

app.Run();

// ── Helpers ───────────────────────────────────────────────────────────────────

static async Task WriteHealthJson(HttpContext ctx, HealthReport report)
{
    ctx.Response.ContentType = "application/json";
    var result = new
    {
        status      = report.Status.ToString(),
        totalMs     = report.TotalDuration.TotalMilliseconds,
        checks      = report.Entries.Select(e => new
        {
            name        = e.Key,
            status      = e.Value.Status.ToString(),
            description = e.Value.Description,
            durationMs  = e.Value.Duration.TotalMilliseconds,
        }),
    };
    await ctx.Response.WriteAsync(JsonSerializer.Serialize(result,
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
}
