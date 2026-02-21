using System.ComponentModel.DataAnnotations;

namespace HikmaAbroad.Models.DTOs;

/// <summary>
/// Request body for creating or updating a student application.
/// </summary>
public class StudentRequest
{
    /// <summary>Student full name (required for submission)</summary>
    /// <example>Araf</example>
    public string? Name { get; set; }

    /// <summary>Email address</summary>
    /// <example>a@gmail.com</example>
    public string? Email { get; set; }

    /// <summary>Phone number</summary>
    /// <example>+8801XXXXXXXX</example>
    public string? Phone { get; set; }

    /// <summary>Country the student is from</summary>
    /// <example>Bangladesh</example>
    public string FromCountry { get; set; } = string.Empty;

    /// <summary>Last academic level completed</summary>
    /// <example>HSC</example>
    public string LastAcademicLevel { get; set; } = string.Empty;

    /// <summary>Client-generated draft token for persistence</summary>
    /// <example>abc123</example>
    public string? DraftToken { get; set; }

    /// <summary>Whether to submit the form (true) or save as draft (false)</summary>
    /// <example>false</example>
    public bool IsSubmitted { get; set; }

    /// <summary>Source of the submission</summary>
    /// <example>web</example>
    public string Source { get; set; } = "web";

    /// <summary>Subject for contact forms</summary>
    public string? Subject { get; set; }

    /// <summary>Message for contact forms</summary>
    public string? Message { get; set; }

    /// <summary>Specific purpose of application</summary>
    public string? Purpose { get; set; }

    /// <summary>Id of target university</summary>
    public string? UniversityId { get; set; }

    /// <summary>Id of target course</summary>
    public string? CourseId { get; set; }
}

/// <summary>
/// Home page aggregated data response.
/// </summary>
public class HomePageResponse
{
    public object? Hero { get; set; }
    public object? Navbar { get; set; }
    public object? Footer { get; set; }
    public object? StudentExperiences { get; set; }
    public object? Destinations { get; set; }
    public object? Services { get; set; }
    public object? Counsellors { get; set; }
    public object? AboutSummary { get; set; }
}
/// <summary>
/// Status update request (isRead, isDone).
/// </summary>
public class StatusUpdateRequest
{
    public bool? IsRead { get; set; }
    public bool? IsDone { get; set; }
}
