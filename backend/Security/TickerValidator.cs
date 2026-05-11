using System.Text.RegularExpressions;

namespace MarketSentiment.Security;

/// <summary>
/// Validates ASX ticker symbols before they are used in external API calls.
/// Prevents injection of unexpected characters into Yahoo Finance URLs.
/// </summary>
internal static class TickerValidator
{
    // ASX tickers: 1–6 uppercase letters/digits, optionally suffixed with .AX
    // Examples: CBA, BHP, CBA.AX, ETF123
    private static readonly Regex ValidPattern = new(
        @"^[A-Z0-9]{1,6}(\.AX)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        matchTimeout: TimeSpan.FromMilliseconds(50));

    private const int MaxLength = 10;

    public static bool IsValid(string? ticker) =>
        !string.IsNullOrWhiteSpace(ticker) &&
        ticker.Length <= MaxLength &&
        ValidPattern.IsMatch(ticker);
}
