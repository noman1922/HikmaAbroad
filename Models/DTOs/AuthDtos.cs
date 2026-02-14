using System.ComponentModel.DataAnnotations;

namespace HikmaAbroad.Models.DTOs;

/// <summary>
/// Login request body.
/// </summary>
public class LoginRequest
{
    /// <summary>Admin email</summary>
    /// <example>admin@hikmaconsult.com</example>
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Admin password</summary>
    /// <example>Admin@123</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Login response with JWT token.
/// </summary>
public class LoginResponse
{
    /// <summary>JWT access token</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Token expiration time</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Admin user name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Admin email</summary>
    public string Email { get; set; } = string.Empty;
}
