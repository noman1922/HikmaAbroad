using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HikmaAbroad.Models.Entities;

/// <summary>
/// Metadata for an uploaded file.
/// </summary>
public class Media
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>Original file name</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Stored file name (unique)</summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>MIME content type</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>File size in bytes</summary>
    public long SizeBytes { get; set; }

    /// <summary>Public URL to access the file</summary>
    public string Url { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
