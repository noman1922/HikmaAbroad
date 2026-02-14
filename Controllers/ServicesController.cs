using HikmaAbroad.Data;
using HikmaAbroad.Helpers;
using HikmaAbroad.Models;
using HikmaAbroad.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HikmaAbroad.Controllers;

/// <summary>
/// Services CRUD. Public GET, admin-only write operations.
/// </summary>
[ApiController]
[Route("api/v1/services")]
[Produces("application/json")]
public class ServicesController : ControllerBase
{
    private readonly MongoDbContext _db;

    public ServicesController(MongoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// List active services (public).
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 60)]
    [ProducesResponseType(typeof(ApiResponse<List<Service>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var services = await _db.Services.Find(s => s.IsActive).ToListAsync();
        return Ok(ApiResponse<List<Service>>.Ok(services));
    }

    /// <summary>
    /// List all services including inactive (admin).
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<List<Service>>), 200)]
    public async Task<IActionResult> GetAllAdmin()
    {
        var services = await _db.Services.Find(_ => true).ToListAsync();
        return Ok(ApiResponse<List<Service>>.Ok(services));
    }

    /// <summary>
    /// Get a service by ID (admin).
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<Service>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id)
    {
        var svc = await _db.Services.Find(s => s.Id == id).FirstOrDefaultAsync();
        if (svc == null) return NotFound(ApiResponse<object>.Fail("Service not found"));
        return Ok(ApiResponse<Service>.Ok(svc));
    }

    /// <summary>
    /// Create a new service (admin). YouTube URL is validated if provided.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<Service>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] Service svc)
    {
        if (!string.IsNullOrEmpty(svc.VideoYouTubeUrl))
        {
            var (isValid, videoId) = ValidationHelper.ParseYouTubeUrl(svc.VideoYouTubeUrl);
            if (!isValid) return BadRequest(ApiResponse<object>.Fail("Invalid YouTube URL"));
            svc.VideoYouTubeId = videoId;
        }

        svc.Id = null;
        svc.CreatedAt = DateTime.UtcNow;
        svc.UpdatedAt = DateTime.UtcNow;
        await _db.Services.InsertOneAsync(svc);
        return CreatedAtAction(nameof(GetById), new { id = svc.Id }, ApiResponse<Service>.Ok(svc));
    }

    /// <summary>
    /// Update a service (admin). YouTube URL is validated if provided.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<Service>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] Service svc)
    {
        var existing = await _db.Services.Find(s => s.Id == id).FirstOrDefaultAsync();
        if (existing == null) return NotFound(ApiResponse<object>.Fail("Service not found"));

        if (!string.IsNullOrEmpty(svc.VideoYouTubeUrl))
        {
            var (isValid, videoId) = ValidationHelper.ParseYouTubeUrl(svc.VideoYouTubeUrl);
            if (!isValid) return BadRequest(ApiResponse<object>.Fail("Invalid YouTube URL"));
            svc.VideoYouTubeId = videoId;
        }

        svc.Id = id;
        svc.CreatedAt = existing.CreatedAt;
        svc.UpdatedAt = DateTime.UtcNow;
        await _db.Services.ReplaceOneAsync(s => s.Id == id, svc);
        return Ok(ApiResponse<Service>.Ok(svc));
    }

    /// <summary>
    /// Delete a service (admin).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.Services.DeleteOneAsync(s => s.Id == id);
        if (result.DeletedCount == 0) return NotFound(ApiResponse<object>.Fail("Service not found"));
        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }
}
