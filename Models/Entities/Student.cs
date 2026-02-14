using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HikmaAbroad.Models.Entities;

/// <summary>
/// A student application (submitted or draft).
/// </summary>
public class Student
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>Student full name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Email address</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Phone number (digits only after normalization)</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>Country the student is from</summary>
    public string FromCountry { get; set; } = string.Empty;

    /// <summary>Last academic level completed</summary>
    public string LastAcademicLevel { get; set; } = string.Empty;

    /// <summary>Whether the form has been submitted</summary>
    public bool IsSubmitted { get; set; }

    /// <summary>Client-generated token for draft persistence</summary>
    public string? DraftToken { get; set; }

    /// <summary>Whether admin has contacted this student</summary>
    public bool IsContacted { get; set; }

    /// <summary>Source of the submission</summary>
    public string Source { get; set; } = "web";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
