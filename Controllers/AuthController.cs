using HikmaAbroad.Models;
using HikmaAbroad.Models.DTOs;
using HikmaAbroad.Services;
using Microsoft.AspNetCore.Mvc;

namespace HikmaAbroad.Controllers;

/// <summary>
/// Authentication endpoints.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Admin login. Returns JWT token on success.
    /// </summary>
    /// <param name="request">Email and password</param>
    /// <returns>JWT token and user info</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _auth.LoginAsync(request);
        if (result == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid email or password"));

        return Ok(ApiResponse<LoginResponse>.Ok(result));
    }
}
