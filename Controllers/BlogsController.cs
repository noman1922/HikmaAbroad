using HikmaAbroad.Data;
using HikmaAbroad.Helpers;
using HikmaAbroad.Models;
using HikmaAbroad.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HikmaAbroad.Controllers;

/// <summary>
/// Blogs CRUD. Public GET, admin/agent write operations.
/// </summary>
[ApiController]
[Route("api/v1/blogs")]
[Produces("application/json")]
public class BlogsController : ControllerBase
{
    private readonly MongoDbContext _db;

    public BlogsController(MongoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// List published blogs (public).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<Blog>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var blogs = await _db.Blogs.Find(b => b.IsPublished)
            .SortByDescending(b => b.CreatedAt)
            .ToListAsync();
        return Ok(ApiResponse<List<Blog>>.Ok(blogs));
    }

    /// <summary>
    /// Get all blogs including drafts (admin/agent).
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<List<Blog>>), 200)]
    public async Task<IActionResult> GetAllAdmin()
    {
        var blogs = await _db.Blogs.Find(_ => true)
            .SortByDescending(b => b.CreatedAt)
            .ToListAsync();
        return Ok(ApiResponse<List<Blog>>.Ok(blogs));
    }

    /// <summary>
    /// Get a blog by ID or Slug.
    /// </summary>
    [HttpGet("{idOrSlug}")]
    [ProducesResponseType(typeof(ApiResponse<Blog>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string idOrSlug)
    {
        var blog = await _db.Blogs.Find(b => b.Id == idOrSlug || b.Slug == idOrSlug).FirstOrDefaultAsync();
        if (blog == null) return NotFound(ApiResponse<object>.Fail("Blog not found"));
        return Ok(ApiResponse<Blog>.Ok(blog));
    }

    /// <summary>
    /// Create a new blog (admin/agent).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<Blog>), 201)]
    public async Task<IActionResult> Create([FromBody] Blog blog)
    {
        blog.Id = null;
        blog.Slug = ValidationHelper.ToSlug(blog.Title);
        blog.CreatedAt = DateTime.UtcNow;
        blog.UpdatedAt = DateTime.UtcNow;
        
        // Ensure unique slug
        var existing = await _db.Blogs.Find(b => b.Slug == blog.Slug).FirstOrDefaultAsync();
        if (existing != null) blog.Slug += "-" + Guid.NewGuid().ToString("N").Substring(0, 4);

        await _db.Blogs.InsertOneAsync(blog);
        return CreatedAtAction(nameof(GetById), new { idOrSlug = blog.Id }, ApiResponse<Blog>.Ok(blog));
    }

    /// <summary>
    /// Update a blog (admin/agent).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<Blog>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] Blog blog)
    {
        var existing = await _db.Blogs.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (existing == null) return NotFound(ApiResponse<object>.Fail("Blog not found"));

        blog.Id = id;
        blog.Slug = ValidationHelper.ToSlug(blog.Title);
        blog.CreatedAt = existing.CreatedAt;
        blog.UpdatedAt = DateTime.UtcNow;

        await _db.Blogs.ReplaceOneAsync(b => b.Id == id, blog);
        return Ok(ApiResponse<Blog>.Ok(blog));
    }

    /// <summary>
    /// Delete a blog (admin/agent).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.Blogs.DeleteOneAsync(b => b.Id == id);
        if (result.DeletedCount == 0) return NotFound(ApiResponse<object>.Fail("Blog not found"));
        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }
}
