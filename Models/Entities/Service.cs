using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HikmaAbroad.Models.Entities;

/// <summary>
/// A service offered by the consultancy.
/// </summary>
public class Service
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>Service title</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Service description</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Full YouTube URL</summary>
    public string VideoYouTubeUrl { get; set; } = string.Empty;

    /// <summary>Parsed YouTube video ID</summary>
    public string VideoYouTubeId { get; set; } = string.Empty;

    /// <summary>Whether this service is publicly visible</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
