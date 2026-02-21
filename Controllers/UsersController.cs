using HikmaAbroad.Data;
using HikmaAbroad.Models;
using HikmaAbroad.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HikmaAbroad.Controllers;

/// <summary>
/// Admin user management. Only accessible by "admin" role.
/// agents cannot access this.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "admin")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly MongoDbContext _db;

    public UsersController(MongoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// List all admin users (admins and agents).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AdminUser>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.AdminUsers.Find(_ => true)
            .Project<AdminUser>(Builders<AdminUser>.Projection.Exclude(u => u.PasswordHash))
            .ToListAsync();
        return Ok(ApiResponse<List<AdminUser>>.Ok(users));
    }

    /// <summary>
    /// Create a new agent or admin.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AdminUser>), 201)]
    public async Task<IActionResult> Create([FromBody] AdminUser user)
    {
        user.Id = null;
        user.Email = user.Email.ToLowerInvariant();
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        
        // Hash password before saving
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

        var existing = await _db.AdminUsers.Find(u => u.Email == user.Email).FirstOrDefaultAsync();
        if (existing != null) return BadRequest(ApiResponse<object>.Fail("Email already exists"));

        await _db.AdminUsers.InsertOneAsync(user);
        user.PasswordHash = ""; // Don't return hash
        return Created("", ApiResponse<AdminUser>.Ok(user));
    }

    /// <summary>
    /// Update a user.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] AdminUser user)
    {
        var existing = await _db.AdminUsers.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (existing == null) return NotFound(ApiResponse<object>.Fail("User not found"));

        var update = Builders<AdminUser>.Update
            .Set(u => u.Name, user.Name)
            .Set(u => u.Email, user.Email.ToLowerInvariant())
            .Set(u => u.Role, user.Role)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        // If password is provided, re-hash it
        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            update = update.Set(u => u.PasswordHash, BCrypt.Net.BCrypt.HashPassword(user.PasswordHash));
        }

        await _db.AdminUsers.UpdateOneAsync(u => u.Id == id, update);
        return Ok(ApiResponse<object>.Ok(new { success = true }));
    }

    /// <summary>
    /// Delete a user.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _db.AdminUsers.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user != null && user.Email == "hikmahabroad@gmail.com")
            return BadRequest(ApiResponse<object>.Fail("Cannot delete primary admin"));

        var result = await _db.AdminUsers.DeleteOneAsync(u => u.Id == id);
        if (result.DeletedCount == 0) return NotFound(ApiResponse<object>.Fail("User not found"));
        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }
}
