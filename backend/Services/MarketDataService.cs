using System.Net.Http.Json;
using System.Text.Json;
using MarketSentiment.Models;

namespace MarketSentiment.Services;

/// <summary>
/// Fetches real-time stock quotes from Yahoo Finance v8 chart API.
/// Ticker symbols are normalised to include the .AX suffix required for ASX stocks.
/// </summary>
public sealed class MarketDataService(HttpClient httpClient, ILogger<MarketDataService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Fetches a real-time quote for a single ticker.
    /// </summary>
    /// <param name="ticker">ASX ticker symbol, with or without .AX suffix.</param>
    /// <param name="cancellationToken">Propagated from the HTTP request lifecycle.</param>
    /// <returns>Populated <see cref="StockQuote"/> or null if the ticker was not found.</returns>
    public async Task<StockQuote?> GetQuoteAsync(string ticker, CancellationToken cancellationToken = default)
    {
        var normalisedTicker = NormaliseTicker(ticker);
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{normalisedTicker}?interval=1d&range=1d";

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return MapToStockQuote(doc, normalisedTicker);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to fetch quote for {Ticker}", normalisedTicker);
            return null;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Unexpected Yahoo Finance response shape for {Ticker}", normalisedTicker);
            return null;
        }
    }

    /// <summary>
    /// Fetches quotes for multiple tickers concurrently, skipping any that fail.
    /// </summary>
    /// <param name="tickers">Collection of ASX ticker symbols.</param>
    /// <param name="cancellationToken">Propagated from the HTTP request lifecycle.</param>
    public async Task<IReadOnlyList<StockQuote>> GetQuotesAsync(
        IEnumerable<string> tickers,
        CancellationToken cancellationToken = default)
    {
        var tasks = tickers.Select(t => GetQuoteAsync(t, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.OfType<StockQuote>().ToList();
    }

    /// <summary>
    /// Fetches OHLCV price history for a single ticker.
    /// </summary>
    /// <param name="ticker">ASX ticker symbol.</param>
    /// <param name="interval">Yahoo Finance interval string: 5m, 1h, 1d, 1wk.</param>
    /// <param name="range">Yahoo Finance range string: 1d, 5d, 1mo, 3mo, 1y.</param>
    /// <param name="cancellationToken">Propagated from the HTTP request lifecycle.</param>
    public async Task<IReadOnlyList<HistoricalPrice>> GetHistoryAsync(
        string ticker,
        string interval,
        string range,
        CancellationToken cancellationToken = default)
    {
        var normalisedTicker = NormaliseTicker(ticker);
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{normalisedTicker}?interval={interval}&range={range}";

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return MapToHistory(doc);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            logger.LogWarning(ex, "Failed to fetch history for {Ticker}", normalisedTicker);
            return [];
        }
    }

    private static IReadOnlyList<HistoricalPrice> MapToHistory(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("chart", out var chart)) return [];
        if (!chart.TryGetProperty("result", out var resultArray)) return [];
        if (resultArray.ValueKind != JsonValueKind.Array || resultArray.GetArrayLength() == 0) return [];

        var result = resultArray[0];
        if (!result.TryGetProperty("timestamp", out var timestamps)) return [];
        if (!result.TryGetProperty("indicators", out var indicators)) return [];
        if (!indicators.TryGetProperty("quote", out var quoteArray)) return [];
        if (quoteArray.GetArrayLength() == 0) return [];

        var quote = quoteArray[0];
        var opens   = quote.TryGetProperty("open",   out var o) ? o : default;
        var highs   = quote.TryGetProperty("high",   out var h) ? h : default;
        var lows    = quote.TryGetProperty("low",    out var l) ? l : default;
        var closes  = quote.TryGetProperty("close",  out var c) ? c : default;
        var volumes = quote.TryGetProperty("volume", out var v) ? v : default;

        var tsArray = timestamps.EnumerateArray().ToArray();
        var results = new List<HistoricalPrice>(tsArray.Length);

        for (int i = 0; i < tsArray.Length; i++)
        {
            // Yahoo returns null entries during market-closed periods — skip them.
            if (closes.ValueKind == JsonValueKind.Array)
            {
                var closeEl = closes[i];
                if (closeEl.ValueKind == JsonValueKind.Null) continue;

                results.Add(new HistoricalPrice
                {
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(tsArray[i].GetInt64()).UtcDateTime,
                    Open      = SafeDecimal(opens,   i),
                    High      = SafeDecimal(highs,   i),
                    Low       = SafeDecimal(lows,    i),
                    Close     = SafeDecimal(closes,  i),
                    Volume    = SafeLong(volumes, i),
                });
            }
        }

        return results;
    }

    private static decimal SafeDecimal(JsonElement arr, int idx)
    {
        if (arr.ValueKind != JsonValueKind.Array) return 0m;
        var el = arr[idx];
        return el.ValueKind == JsonValueKind.Number ? el.GetDecimal() : 0m;
    }

    private static long SafeLong(JsonElement arr, int idx)
    {
        if (arr.ValueKind != JsonValueKind.Array) return 0;
        var el = arr[idx];
        return el.ValueKind == JsonValueKind.Number ? el.GetInt64() : 0;
    }

    // Yahoo Finance requires the .AX suffix for ASX-listed securities.
    private static string NormaliseTicker(string ticker) =>
        ticker.EndsWith(".AX", StringComparison.OrdinalIgnoreCase)
            ? ticker.ToUpperInvariant()
            : $"{ticker.ToUpperInvariant()}.AX";

    private static StockQuote? MapToStockQuote(JsonDocument doc, string ticker)
    {
        // Navigate: chart -> result[0] -> meta
        if (!doc.RootElement.TryGetProperty("chart", out var chart)) return null;
        if (!chart.TryGetProperty("result", out var resultArray)) return null;
        if (resultArray.ValueKind != JsonValueKind.Array || resultArray.GetArrayLength() == 0) return null;

        var result = resultArray[0];
        if (!result.TryGetProperty("meta", out var meta)) return null;

        var price = meta.TryGetProperty("regularMarketPrice", out var p) ? p.GetDecimal() : 0m;
        var previousClose = meta.TryGetProperty("chartPreviousClose", out var pc) ? pc.GetDecimal() : price;
        var change = price - previousClose;
        var changePercent = previousClose != 0 ? (change / previousClose) * 100m : 0m;

        return new StockQuote
        {
            Ticker = ticker,
            CompanyName = meta.TryGetProperty("longName", out var name) ? name.GetString() ?? ticker : ticker,
            Price = price,
            Change = Math.Round(change, 3),
            ChangePercent = Math.Round(changePercent, 2),
            Volume = meta.TryGetProperty("regularMarketVolume", out var vol) ? vol.GetInt64() : 0,
            High52Week = meta.TryGetProperty("fiftyTwoWeekHigh", out var h52) ? h52.GetDecimal() : 0m,
            Low52Week = meta.TryGetProperty("fiftyTwoWeekLow", out var l52) ? l52.GetDecimal() : 0m,
            MarketCap = meta.TryGetProperty("marketCap", out var mc) ? mc.GetInt64() : 0,
            FetchedAt = DateTime.UtcNow
        };
    }
}
