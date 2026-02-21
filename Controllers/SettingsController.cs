using HikmaAbroad.Data;
using HikmaAbroad.Models;
using HikmaAbroad.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HikmaAbroad.Controllers;

/// <summary>
/// Site settings management (admin).
/// </summary>
[ApiController]
[Route("api/v1/settings")]
[Produces("application/json")]
public class SettingsController : ControllerBase
{
    private readonly MongoDbContext _db;

    public SettingsController(MongoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get current site settings.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<SiteSettings>), 200)]
    public async Task<IActionResult> Get()
    {
        var settings = await _db.SiteSettings.Find(s => s.Id == "settings").FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new SiteSettings();
            await _db.SiteSettings.InsertOneAsync(settings);
        }
        return Ok(ApiResponse<SiteSettings>.Ok(settings));
    }

    /// <summary>
    /// Update site settings (hero, footer, navbar, etc.).
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<SiteSettings>), 200)]
    public async Task<IActionResult> Update([FromBody] SiteSettings settings)
    {
        settings.Id = "settings";
        settings.UpdatedAt = DateTime.UtcNow;

        var existing = await _db.SiteSettings.Find(s => s.Id == "settings").FirstOrDefaultAsync();
        if (existing != null)
        {
            settings.CreatedAt = existing.CreatedAt;
            await _db.SiteSettings.ReplaceOneAsync(s => s.Id == "settings", settings);
        }
        else
        {
            await _db.SiteSettings.InsertOneAsync(settings);
        }

        return Ok(ApiResponse<SiteSettings>.Ok(settings));
    }
}
