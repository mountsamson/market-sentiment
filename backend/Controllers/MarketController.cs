using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MarketSentiment.Models;
using MarketSentiment.Security;
using MarketSentiment.Services;

namespace MarketSentiment.Controllers;

[ApiController]
[Route("api/market")]
[EnableRateLimiting("api")]
public sealed class MarketController(
    MarketDataService marketDataService,
    NewsService newsService,
    ILogger<MarketController> logger) : ControllerBase
{
    // Only the intervals and ranges supported by Yahoo Finance v8 chart API.
    private static readonly HashSet<string> ValidIntervals =
        ["5m", "15m", "1h", "1d", "1wk", "1mo"];

    private static readonly HashSet<string> ValidRanges =
        ["1d", "5d", "1mo", "3mo", "6mo", "1y", "2y", "5y"];

    [HttpGet("{ticker}")]
    [ProducesResponseType<StockQuote>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockQuote>> GetQuote(string ticker, CancellationToken cancellationToken)
    {
        if (!TickerValidator.IsValid(ticker))
            return BadRequest(new { error = "Invalid ticker symbol." });

        logger.LogInformation("Quote requested for {Ticker}", ticker);

        var quote = await marketDataService.GetQuoteAsync(ticker, cancellationToken);

        if (quote is null)
            return NotFound(new { error = $"No data found for ticker '{ticker}'." });

        return Ok(quote);
    }

    [HttpGet("{ticker}/history")]
    [ProducesResponseType<IReadOnlyList<HistoricalPrice>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<HistoricalPrice>>> GetHistory(
        string ticker,
        [FromQuery] string interval = "1d",
        [FromQuery] string range = "1mo",
        CancellationToken cancellationToken = default)
    {
        if (!TickerValidator.IsValid(ticker))
            return BadRequest(new { error = "Invalid ticker symbol." });

        if (!ValidIntervals.Contains(interval))
            return BadRequest(new { error = $"Invalid interval. Allowed: {string.Join(", ", ValidIntervals)}" });

        if (!ValidRanges.Contains(range))
            return BadRequest(new { error = $"Invalid range. Allowed: {string.Join(", ", ValidRanges)}" });

        var history = await marketDataService.GetHistoryAsync(ticker, interval, range, cancellationToken);
        return Ok(history);
    }

    [HttpGet("{ticker}/news")]
    [ProducesResponseType<IReadOnlyList<NewsItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<NewsItem>>> GetNews(string ticker, CancellationToken cancellationToken)
    {
        if (!TickerValidator.IsValid(ticker))
            return BadRequest(new { error = "Invalid ticker symbol." });

        var news = await newsService.GetNewsAsync(ticker, cancellationToken: cancellationToken);
        return Ok(news);
    }
}
