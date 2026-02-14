using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HikmaAbroad.Models.Entities;

/// <summary>
/// A study-abroad destination country.
/// </summary>
public class Destination
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>Country name</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>URL-friendly slug</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Whether this destination is publicly visible</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Image URL for the destination</summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Description of the destination</summary>
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
