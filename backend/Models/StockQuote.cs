namespace MarketSentiment.Models;

/// <summary>
/// Represents a real-time stock quote sourced from Yahoo Finance.
/// </summary>
public sealed class StockQuote
{
    /// <summary>ASX ticker symbol, e.g. "CBA.AX".</summary>
    public required string Ticker { get; init; }

    /// <summary>Display name of the company.</summary>
    public required string CompanyName { get; init; }

    /// <summary>Current market price in AUD.</summary>
    public decimal Price { get; init; }

    /// <summary>Absolute change from previous close.</summary>
    public decimal Change { get; init; }

    /// <summary>Percentage change from previous close.</summary>
    public decimal ChangePercent { get; init; }

    /// <summary>Trading volume for the current session.</summary>
    public long Volume { get; init; }

    /// <summary>52-week high price.</summary>
    public decimal High52Week { get; init; }

    /// <summary>52-week low price.</summary>
    public decimal Low52Week { get; init; }

    /// <summary>Market capitalisation in AUD.</summary>
    public long MarketCap { get; init; }

    /// <summary>UTC timestamp of when this quote was fetched.</summary>
    public DateTime FetchedAt { get; init; }
}
