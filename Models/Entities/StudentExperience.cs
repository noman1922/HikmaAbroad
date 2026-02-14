using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HikmaAbroad.Models.Entities;

/// <summary>
/// A student testimonial / experience entry.
/// </summary>
public class StudentExperience
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>Student name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short summary text</summary>
    public string ShortText { get; set; } = string.Empty;

    /// <summary>Full testimonial text</summary>
    public string FullText { get; set; } = string.Empty;

    /// <summary>Student photo URL</summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Display order (lower = first)</summary>
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
