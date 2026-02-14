using HikmaAbroad.Data;
using HikmaAbroad.Helpers;
using HikmaAbroad.Models;
using HikmaAbroad.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HikmaAbroad.Controllers;

/// <summary>
/// Destinations CRUD. Public GET, admin-only write operations.
/// </summary>
[ApiController]
[Route("api/v1/destinations")]
[Produces("application/json")]
public class DestinationsController : ControllerBase
{
    private readonly MongoDbContext _db;

    public DestinationsController(MongoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// List active destinations (public).
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 60)]
    [ProducesResponseType(typeof(ApiResponse<List<Destination>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var destinations = await _db.Destinations.Find(d => d.IsActive).ToListAsync();
        return Ok(ApiResponse<List<Destination>>.Ok(destinations));
    }

    /// <summary>
    /// Get all destinations including inactive (admin).
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<List<Destination>>), 200)]
    public async Task<IActionResult> GetAllAdmin()
    {
        var destinations = await _db.Destinations.Find(_ => true).ToListAsync();
        return Ok(ApiResponse<List<Destination>>.Ok(destinations));
    }

    /// <summary>
    /// Get a destination by ID (admin).
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<Destination>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id)
    {
        var dest = await _db.Destinations.Find(d => d.Id == id).FirstOrDefaultAsync();
        if (dest == null) return NotFound(ApiResponse<object>.Fail("Destination not found"));
        return Ok(ApiResponse<Destination>.Ok(dest));
    }

    /// <summary>
    /// Create a new destination (admin).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<Destination>), 201)]
    public async Task<IActionResult> Create([FromBody] Destination dest)
    {
        dest.Id = null;
        dest.Slug = ValidationHelper.ToSlug(dest.Country);
        dest.CreatedAt = DateTime.UtcNow;
        dest.UpdatedAt = DateTime.UtcNow;
        await _db.Destinations.InsertOneAsync(dest);
        return CreatedAtAction(nameof(GetById), new { id = dest.Id }, ApiResponse<Destination>.Ok(dest));
    }

    /// <summary>
    /// Update a destination (admin).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<Destination>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] Destination dest)
    {
        var existing = await _db.Destinations.Find(d => d.Id == id).FirstOrDefaultAsync();
        if (existing == null) return NotFound(ApiResponse<object>.Fail("Destination not found"));

        dest.Id = id;
        dest.Slug = ValidationHelper.ToSlug(dest.Country);
        dest.CreatedAt = existing.CreatedAt;
        dest.UpdatedAt = DateTime.UtcNow;
        await _db.Destinations.ReplaceOneAsync(d => d.Id == id, dest);
        return Ok(ApiResponse<Destination>.Ok(dest));
    }

    /// <summary>
    /// Delete a destination (admin).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.Destinations.DeleteOneAsync(d => d.Id == id);
        if (result.DeletedCount == 0) return NotFound(ApiResponse<object>.Fail("Destination not found"));
        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }
}
