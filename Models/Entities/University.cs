using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HikmaAbroad.Models.Entities;

/// <summary>
/// A university entity.
/// </summary>
[BsonIgnoreExtraElements]
public class University
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("location")]
    public string Location { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("logoUrl")]
    public string LogoUrl { get; set; } = string.Empty;

    [BsonElement("bannerUrl")]
    public string BannerUrl { get; set; } = string.Empty;

    [BsonElement("offerLetterApplicable")]
    public bool OfferLetterApplicable { get; set; } = true;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
