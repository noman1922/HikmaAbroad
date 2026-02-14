using System.Text.RegularExpressions;

namespace HikmaAbroad.Helpers;

public static partial class ValidationHelper
{
    private static readonly Regex YouTubeRegex = new(
        @"(?:https?://)?(?:www\.)?(?:youtube\.com/watch\?v=|youtu\.be/)([a-zA-Z0-9_-]{11})",
        RegexOptions.Compiled);

    /// <summary>
    /// Validates a YouTube URL and extracts the video ID.
    /// </summary>
    public static (bool IsValid, string VideoId) ParseYouTubeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return (false, string.Empty);

        var match = YouTubeRegex.Match(url);
        return match.Success
            ? (true, match.Groups[1].Value)
            : (false, string.Empty);
    }

    /// <summary>
    /// Normalizes a phone number to digits only (preserving leading +).
    /// </summary>
    public static string NormalizePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
        var prefix = phone.StartsWith('+') ? "+" : "";
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return prefix + digits;
    }

    /// <summary>
    /// Sanitizes a string by trimming and removing control characters.
    /// </summary>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return new string(input.Trim().Where(c => !char.IsControl(c) || c == '\n' || c == '\r').ToArray());
    }

    /// <summary>
    /// Generates a URL-friendly slug from a string.
    /// </summary>
    public static string ToSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var slug = input.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }
}
