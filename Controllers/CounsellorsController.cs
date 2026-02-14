using HikmaAbroad.Data;
using HikmaAbroad.Models;
using HikmaAbroad.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HikmaAbroad.Controllers;

/// <summary>
/// Counsellors CRUD. Public GET, admin-only write operations.
/// </summary>
[ApiController]
[Route("api/v1/counsellors")]
[Produces("application/json")]
public class CounsellorsController : ControllerBase
{
    private readonly MongoDbContext _db;

    public CounsellorsController(MongoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// List all counsellors (public).
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 60)]
    [ProducesResponseType(typeof(ApiResponse<List<Counsellor>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var counsellors = await _db.Counsellors.Find(_ => true).ToListAsync();
        return Ok(ApiResponse<List<Counsellor>>.Ok(counsellors));
    }

    /// <summary>
    /// Get a counsellor by ID (admin).
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<Counsellor>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id)
    {
        var c = await _db.Counsellors.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (c == null) return NotFound(ApiResponse<object>.Fail("Counsellor not found"));
        return Ok(ApiResponse<Counsellor>.Ok(c));
    }

    /// <summary>
    /// Create a new counsellor (admin).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<Counsellor>), 201)]
    public async Task<IActionResult> Create([FromBody] Counsellor c)
    {
        c.Id = null;
        c.CreatedAt = DateTime.UtcNow;
        c.UpdatedAt = DateTime.UtcNow;
        await _db.Counsellors.InsertOneAsync(c);
        return CreatedAtAction(nameof(GetById), new { id = c.Id }, ApiResponse<Counsellor>.Ok(c));
    }

    /// <summary>
    /// Update a counsellor (admin).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<Counsellor>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] Counsellor c)
    {
        var existing = await _db.Counsellors.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (existing == null) return NotFound(ApiResponse<object>.Fail("Counsellor not found"));

        c.Id = id;
        c.CreatedAt = existing.CreatedAt;
        c.UpdatedAt = DateTime.UtcNow;
        await _db.Counsellors.ReplaceOneAsync(x => x.Id == id, c);
        return Ok(ApiResponse<Counsellor>.Ok(c));
    }

    /// <summary>
    /// Delete a counsellor (admin).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.Counsellors.DeleteOneAsync(x => x.Id == id);
        if (result.DeletedCount == 0) return NotFound(ApiResponse<object>.Fail("Counsellor not found"));
        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }
}
