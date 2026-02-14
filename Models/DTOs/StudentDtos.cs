using System.ComponentModel.DataAnnotations;

namespace HikmaAbroad.Models.DTOs;

/// <summary>
/// Request body for creating or updating a student application.
/// </summary>
public class StudentRequest
{
    /// <summary>Student full name (required for submission)</summary>
    /// <example>Araf</example>
    [MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Email address</summary>
    /// <example>a@gmail.com</example>
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Phone number</summary>
    /// <example>+8801XXXXXXXX</example>
    public string Phone { get; set; } = string.Empty;

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
