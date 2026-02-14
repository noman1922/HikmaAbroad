using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HikmaAbroad.Models.Entities;

/// <summary>
/// A counsellor / team member.
/// </summary>
public class Counsellor
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>Counsellor full name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Job title</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Photo URL</summary>
    public string PhotoUrl { get; set; } = string.Empty;

    /// <summary>Contact info (phone/email)</summary>
    public string Contact { get; set; } = string.Empty;

    /// <summary>Short biography</summary>
    public string Bio { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
