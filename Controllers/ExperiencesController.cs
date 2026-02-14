using HikmaAbroad.Data;
using HikmaAbroad.Models;
using HikmaAbroad.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HikmaAbroad.Controllers;

/// <summary>
/// Student Experiences CRUD. Admin-only write operations.
/// </summary>
[ApiController]
[Route("api/v1/experiences")]
[Produces("application/json")]
public class ExperiencesController : ControllerBase
{
    private readonly MongoDbContext _db;

    public ExperiencesController(MongoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// List all student experiences ordered by display order.
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 60)]
    [ProducesResponseType(typeof(ApiResponse<List<StudentExperience>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.StudentExperiences.Find(_ => true).SortBy(e => e.DisplayOrder).ToListAsync();
        return Ok(ApiResponse<List<StudentExperience>>.Ok(list));
    }

    /// <summary>
    /// Get an experience by ID (admin).
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<StudentExperience>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id)
    {
        var exp = await _db.StudentExperiences.Find(e => e.Id == id).FirstOrDefaultAsync();
        if (exp == null) return NotFound(ApiResponse<object>.Fail("Experience not found"));
        return Ok(ApiResponse<StudentExperience>.Ok(exp));
    }

    /// <summary>
    /// Create a new student experience (admin).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<StudentExperience>), 201)]
    public async Task<IActionResult> Create([FromBody] StudentExperience exp)
    {
        exp.Id = null;
        exp.CreatedAt = DateTime.UtcNow;
        exp.UpdatedAt = DateTime.UtcNow;
        await _db.StudentExperiences.InsertOneAsync(exp);
        return CreatedAtAction(nameof(GetById), new { id = exp.Id }, ApiResponse<StudentExperience>.Ok(exp));
    }

    /// <summary>
    /// Update a student experience (admin).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<StudentExperience>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] StudentExperience exp)
    {
        var existing = await _db.StudentExperiences.Find(e => e.Id == id).FirstOrDefaultAsync();
        if (existing == null) return NotFound(ApiResponse<object>.Fail("Experience not found"));

        exp.Id = id;
        exp.CreatedAt = existing.CreatedAt;
        exp.UpdatedAt = DateTime.UtcNow;
        await _db.StudentExperiences.ReplaceOneAsync(e => e.Id == id, exp);
        return Ok(ApiResponse<StudentExperience>.Ok(exp));
    }

    /// <summary>
    /// Delete a student experience (admin).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.StudentExperiences.DeleteOneAsync(e => e.Id == id);
        if (result.DeletedCount == 0) return NotFound(ApiResponse<object>.Fail("Experience not found"));
        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }
}
