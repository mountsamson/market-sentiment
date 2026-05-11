namespace MarketSentiment.Models;

/// <summary>
/// Represents a single financial news headline associated with a ticker.
/// </summary>
public sealed class NewsItem
{
    /// <summary>Article headline.</summary>
    public required string Headline { get; init; }

    /// <summary>News source publisher name.</summary>
    public required string Publisher { get; init; }

    /// <summary>URL to the full article.</summary>
    public required string Url { get; init; }

    /// <summary>UTC publish time of the article.</summary>
    public DateTime PublishedAt { get; init; }

    /// <summary>Thumbnail image URL, if available.</summary>
    public string? ThumbnailUrl { get; init; }
}
