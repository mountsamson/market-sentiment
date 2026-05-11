namespace MarketSentiment.Models;

/// <summary>
/// A single OHLCV candle point returned by the history endpoint.
/// </summary>
public sealed class HistoricalPrice
{
    /// <summary>UTC timestamp for this candle.</summary>
    public DateTime Timestamp { get; init; }

    public decimal Open   { get; init; }
    public decimal High   { get; init; }
    public decimal Low    { get; init; }
    public decimal Close  { get; init; }
    public long    Volume { get; init; }
}
