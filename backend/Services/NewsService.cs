using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using MarketSentiment.Models;

namespace MarketSentiment.Services;

public sealed class NewsService(HttpClient httpClient, IMemoryCache cache, ILogger<NewsService> logger)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<NewsItem>> GetNewsAsync(
        string ticker,
        int maxItems = 5,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"news:{ticker.ToUpperInvariant()}:{maxItems}";
        if (cache.TryGetValue(cacheKey, out IReadOnlyList<NewsItem>? cached))
            return cached!;

        var query = ticker.Replace(".AX", "", StringComparison.OrdinalIgnoreCase);
        var url = $"https://query1.finance.yahoo.com/v1/finance/search?q={query}&newsCount={maxItems}&quotesCount=0";

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var result = MapToNewsItems(doc);
            cache.Set(cacheKey, result, CacheTtl);
            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to fetch news for {Ticker}", ticker);
            return [];
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Unexpected news response shape for {Ticker}", ticker);
            return [];
        }
    }

    private static IReadOnlyList<NewsItem> MapToNewsItems(JsonDocument doc)
    {
        // Yahoo Finance v1/finance/search returns news at the root level: { "news": [...] }
        if (!doc.RootElement.TryGetProperty("news", out var newsArray)) return [];
        if (newsArray.ValueKind != JsonValueKind.Array || newsArray.GetArrayLength() == 0) return [];

        var items = new List<NewsItem>();
        foreach (var article in newsArray.EnumerateArray())
        {
            var headline = article.TryGetProperty("title", out var t) ? t.GetString() : null;
            var publisher = article.TryGetProperty("publisher", out var pub) ? pub.GetString() : null;
            var url = article.TryGetProperty("link", out var l) ? l.GetString() : null;

            if (headline is null || url is null) continue;

            var publishedAt = article.TryGetProperty("providerPublishTime", out var ts)
                ? DateTimeOffset.FromUnixTimeSeconds(ts.GetInt64()).UtcDateTime
                : DateTime.UtcNow;

            string? thumbnailUrl = null;
            if (article.TryGetProperty("thumbnail", out var thumb) &&
                thumb.TryGetProperty("resolutions", out var resolutions) &&
                resolutions.GetArrayLength() > 0)
            {
                thumbnailUrl = resolutions[0].TryGetProperty("url", out var imgUrl)
                    ? imgUrl.GetString()
                    : null;
            }

            items.Add(new NewsItem
            {
                Headline = headline,
                Publisher = publisher ?? "Unknown",
                Url = url,
                PublishedAt = publishedAt,
                ThumbnailUrl = thumbnailUrl
            });
        }

        return items;
    }
}
