using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HikmaAbroad.Models.Entities;

/// <summary>
/// An editable content page (About, etc.).
/// </summary>
public class Page
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>Unique page key (e.g., "about")</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Page title</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>HTML content of the page</summary>
    public string HtmlContent { get; set; } = string.Empty;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
