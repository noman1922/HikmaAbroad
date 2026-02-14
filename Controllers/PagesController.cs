using HikmaAbroad.Data;
using HikmaAbroad.Models;
using HikmaAbroad.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HikmaAbroad.Controllers;

/// <summary>
/// Editable pages CRUD (About, etc.). Admin-only write operations.
/// </summary>
[ApiController]
[Route("api/v1/pages")]
[Produces("application/json")]
public class PagesController : ControllerBase
{
    private readonly MongoDbContext _db;

    public PagesController(MongoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get a page by its key (public).
    /// </summary>
    /// <param name="key">Page key, e.g. "about"</param>
    [HttpGet("{key}")]
    [ResponseCache(Duration = 60)]
    [ProducesResponseType(typeof(ApiResponse<Page>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByKey(string key)
    {
        var page = await _db.Pages.Find(p => p.Key == key.ToLowerInvariant()).FirstOrDefaultAsync();
        if (page == null) return NotFound(ApiResponse<object>.Fail("Page not found"));
        return Ok(ApiResponse<Page>.Ok(page));
    }

    /// <summary>
    /// List all pages (admin).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<List<Page>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var pages = await _db.Pages.Find(_ => true).ToListAsync();
        return Ok(ApiResponse<List<Page>>.Ok(pages));
    }

    /// <summary>
    /// Create or update a page by key (admin).
    /// </summary>
    [HttpPut("{key}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<Page>), 200)]
    public async Task<IActionResult> Upsert(string key, [FromBody] Page page)
    {
        page.Key = key.ToLowerInvariant();
        page.LastUpdated = DateTime.UtcNow;
        page.UpdatedAt = DateTime.UtcNow;

        var existing = await _db.Pages.Find(p => p.Key == page.Key).FirstOrDefaultAsync();
        if (existing != null)
        {
            page.Id = existing.Id;
            page.CreatedAt = existing.CreatedAt;
            await _db.Pages.ReplaceOneAsync(p => p.Id == existing.Id, page);
        }
        else
        {
            page.Id = null;
            page.CreatedAt = DateTime.UtcNow;
            await _db.Pages.InsertOneAsync(page);
        }

        return Ok(ApiResponse<Page>.Ok(page));
    }

    /// <summary>
    /// Delete a page (admin).
    /// </summary>
    [HttpDelete("{key}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string key)
    {
        var result = await _db.Pages.DeleteOneAsync(p => p.Key == key.ToLowerInvariant());
        if (result.DeletedCount == 0) return NotFound(ApiResponse<object>.Fail("Page not found"));
        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }
}
