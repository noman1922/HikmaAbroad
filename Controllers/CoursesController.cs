using HikmaAbroad.Data;
using HikmaAbroad.Helpers;
using HikmaAbroad.Models;
using HikmaAbroad.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HikmaAbroad.Controllers;

/// <summary>
/// Courses CRUD. Public GET, admin-only write operations.
/// </summary>
[ApiController]
[Route("api/v1/courses")]
[Produces("application/json")]
public class CoursesController : ControllerBase
{
    private readonly MongoDbContext _db;

    public CoursesController(MongoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// List active courses (public).
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 60)]
    [ProducesResponseType(typeof(ApiResponse<List<Course>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var courses = await _db.Courses.Find(c => c.IsActive).ToListAsync();
        return Ok(ApiResponse<List<Course>>.Ok(courses));
    }

    /// <summary>
    /// Get all courses including inactive (admin).
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<List<Course>>), 200)]
    public async Task<IActionResult> GetAllAdmin()
    {
        var courses = await _db.Courses.Find(_ => true).ToListAsync();
        return Ok(ApiResponse<List<Course>>.Ok(courses));
    }

    /// <summary>
    /// Get a course by ID (admin).
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<Course>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id)
    {
        var course = await _db.Courses.Find(c => c.Id == id).FirstOrDefaultAsync();
        if (course == null) return NotFound(ApiResponse<object>.Fail("Course not found"));
        return Ok(ApiResponse<Course>.Ok(course));
    }

    /// <summary>
    /// Create a new course (admin).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<Course>), 201)]
    public async Task<IActionResult> Create([FromBody] Course course)
    {
        course.Id = null;
        course.Slug = ValidationHelper.ToSlug(course.Name);
        course.CreatedAt = DateTime.UtcNow;
        course.UpdatedAt = DateTime.UtcNow;

        // Automatically populate UniversityName if missing or for consistency
        if (!string.IsNullOrEmpty(course.UniversityId) && ObjectId.TryParse(course.UniversityId, out _))
        {
            var uni = await _db.Universities.Find(u => u.Id == course.UniversityId).FirstOrDefaultAsync();
            if (uni != null)
            {
                course.UniversityName = uni.Name;
            }
        }

        await _db.Courses.InsertOneAsync(course);
        return CreatedAtAction(nameof(GetById), new { id = course.Id }, ApiResponse<Course>.Ok(course));
    }

    /// <summary>
    /// Update a course (admin).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<Course>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] Course course)
    {
        var existing = await _db.Courses.Find(c => c.Id == id).FirstOrDefaultAsync();
        if (existing == null) return NotFound(ApiResponse<object>.Fail("Course not found"));

        course.Id = id;
        course.Slug = ValidationHelper.ToSlug(course.Name);
        course.CreatedAt = existing.CreatedAt;
        course.UpdatedAt = DateTime.UtcNow;

        // Automatically populate UniversityName if UniversityId changed or for consistency
        if (!string.IsNullOrEmpty(course.UniversityId) && ObjectId.TryParse(course.UniversityId, out _))
        {
            var uni = await _db.Universities.Find(u => u.Id == course.UniversityId).FirstOrDefaultAsync();
            if (uni != null)
            {
                course.UniversityName = uni.Name;
            }
        }

        await _db.Courses.ReplaceOneAsync(c => c.Id == id, course);
        return Ok(ApiResponse<Course>.Ok(course));
    }

    /// <summary>
    /// Delete a course (admin).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.Courses.DeleteOneAsync(c => c.Id == id);
        if (result.DeletedCount == 0) return NotFound(ApiResponse<object>.Fail("Course not found"));
        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }
}
