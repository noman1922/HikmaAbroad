using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HikmaAbroad.Models.Entities;

/// <summary>
/// A contact form submission (submitted or draft).
/// </summary>
public class Contact
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>Sender full name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Email address</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Phone number</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>Subject of the message</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>The message content</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Whether the form has been submitted</summary>
    public bool IsSubmitted { get; set; }

    /// <summary>Client-generated token for draft persistence</summary>
    public string? DraftToken { get; set; }

    /// <summary>Whether admin has responded to this contact</summary>
    public bool IsResponded { get; set; }

    /// <summary>Whether admin has marked this as done/processed</summary>
    public bool IsDone { get; set; }

    /// <summary>Whether the inquiry has been read by admin</summary>
    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
