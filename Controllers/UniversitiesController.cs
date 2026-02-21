using HikmaAbroad.Data;
using HikmaAbroad.Helpers;
using HikmaAbroad.Models;
using HikmaAbroad.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HikmaAbroad.Controllers;

/// <summary>
/// Universities CRUD. Public GET, admin-only write operations.
/// </summary>
[ApiController]
[Route("api/v1/universities")]
[Produces("application/json")]
public class UniversitiesController : ControllerBase
{
    private readonly MongoDbContext _db;

    public UniversitiesController(MongoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// List active universities (public).
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 60)]
    [ProducesResponseType(typeof(ApiResponse<List<University>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var universities = await _db.Universities.Find(u => u.IsActive).ToListAsync();
        return Ok(ApiResponse<List<University>>.Ok(universities));
    }

    /// <summary>
    /// Get all universities including inactive (admin).
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<List<University>>), 200)]
    public async Task<IActionResult> GetAllAdmin()
    {
        var universities = await _db.Universities.Find(_ => true).ToListAsync();
        return Ok(ApiResponse<List<University>>.Ok(universities));
    }

    /// <summary>
    /// Get a university by ID (admin).
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<University>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id)
    {
        var uni = await _db.Universities.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (uni == null) return NotFound(ApiResponse<object>.Fail("University not found"));
        return Ok(ApiResponse<University>.Ok(uni));
    }

    /// <summary>
    /// Create a new university (admin).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<University>), 201)]
    public async Task<IActionResult> Create([FromBody] University uni)
    {
        uni.Id = null;
        uni.Slug = ValidationHelper.ToSlug(uni.Name);
        uni.CreatedAt = DateTime.UtcNow;
        uni.UpdatedAt = DateTime.UtcNow;
        await _db.Universities.InsertOneAsync(uni);
        return CreatedAtAction(nameof(GetById), new { id = uni.Id }, ApiResponse<University>.Ok(uni));
    }

    /// <summary>
    /// Update a university (admin).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<University>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] University uni)
    {
        var existing = await _db.Universities.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (existing == null) return NotFound(ApiResponse<object>.Fail("University not found"));

        uni.Id = id;
        uni.Slug = ValidationHelper.ToSlug(uni.Name);
        uni.CreatedAt = existing.CreatedAt;
        uni.UpdatedAt = DateTime.UtcNow;
        await _db.Universities.ReplaceOneAsync(u => u.Id == id, uni);
        return Ok(ApiResponse<University>.Ok(uni));
    }

    /// <summary>
    /// Delete a university (admin).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.Universities.DeleteOneAsync(u => u.Id == id);
        if (result.DeletedCount == 0) return NotFound(ApiResponse<object>.Fail("University not found"));
        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }
}
