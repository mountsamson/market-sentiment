using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MarketSentiment.Security;
using MarketSentiment.Services;

namespace MarketSentiment.Controllers;

[ApiController]
[Route("api/stream")]
[EnableRateLimiting("stream")]
public sealed class StreamController(
    MarketDataService marketDataService,
    ILogger<StreamController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [HttpGet]
    public async Task StreamQuotes([FromQuery] string tickers, CancellationToken cancellationToken)
    {
        // Parse, validate, and cap the ticker list.
        var tickerList = (tickers ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(TickerValidator.IsValid)
            .Take(10)
            .ToList();

        if (tickerList.Count == 0)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            Response.ContentType = "application/json";
            await Response.WriteAsync("{\"error\":\"No valid tickers provided.\"}", cancellationToken);
            return;
        }

        Response.Headers.Append("Content-Type",      "text/event-stream");
        Response.Headers.Append("Cache-Control",     "no-cache, no-store");
        Response.Headers.Append("X-Accel-Buffering", "no");

        logger.LogInformation("SSE stream opened for {Tickers}", string.Join(", ", tickerList));

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var quotes = await marketDataService.GetQuotesAsync(tickerList, cancellationToken);
                var json    = JsonSerializer.Serialize(quotes, JsonOptions);
                var payload = $"data: {json}\n\n";

                await Response.WriteAsync(payload, Encoding.UTF8, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("SSE stream closed for {Tickers}", string.Join(", ", tickerList));
        }
    }
}
