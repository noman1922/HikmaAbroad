using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HikmaAbroad.Models.Entities;

/// <summary>
/// A course entity.
/// </summary>
[BsonIgnoreExtraElements]
public class Course
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("universityId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UniversityId { get; set; } = string.Empty;

    [BsonElement("universityName")]
    public string UniversityName { get; set; } = string.Empty;

    [BsonElement("level")]
    public string Level { get; set; } = string.Empty;

    [BsonElement("duration")]
    public string Duration { get; set; } = string.Empty;

    [BsonElement("tuitionFee")]
    public string TuitionFee { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
