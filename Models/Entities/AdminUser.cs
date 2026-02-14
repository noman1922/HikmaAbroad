using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HikmaAbroad.Models.Entities;

/// <summary>
/// An admin user for the CMS.
/// </summary>
public class AdminUser
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>Admin email (used as login)</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>BCrypt hashed password</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Display name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Role (always "admin" for now)</summary>
    public string Role { get; set; } = "admin";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
