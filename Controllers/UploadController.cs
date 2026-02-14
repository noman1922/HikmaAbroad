using HikmaAbroad.Models;
using HikmaAbroad.Models.Entities;
using HikmaAbroad.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HikmaAbroad.Controllers;

/// <summary>
/// File upload endpoint (admin).
/// </summary>
[ApiController]
[Route("api/v1/upload")]
[Authorize(Roles = "admin")]
public class UploadController : ControllerBase
{
    private readonly IFileStorageService _storage;
    private readonly IConfiguration _config;

    public UploadController(IFileStorageService storage, IConfiguration config)
    {
        _storage = storage;
        _config = config;
    }

    /// <summary>
    /// Upload an image file. Returns the public URL.
    /// Allowed types: jpg, png, webp. Max size: 5MB.
    /// </summary>
    /// <param name="file">Image file to upload</param>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<Media>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [RequestSizeLimit(6_000_000)] // slightly above 5MB to account for overhead
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file provided"));

        var maxSizeMB = _config.GetValue<int>("Upload:MaxFileSizeMB", 5);
        if (file.Length > maxSizeMB * 1024 * 1024)
            return BadRequest(ApiResponse<object>.Fail($"File exceeds maximum size of {maxSizeMB}MB"));

        var allowedTypes = _config.GetSection("Upload:AllowedImageTypes").Get<string[]>()
            ?? new[] { "image/jpeg", "image/png", "image/webp" };

        if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest(ApiResponse<object>.Fail($"File type '{file.ContentType}' not allowed. Allowed: {string.Join(", ", allowedTypes)}"));

        using var stream = file.OpenReadStream();
        var media = await _storage.UploadAsync(stream, file.FileName, file.ContentType, file.Length);

        return Ok(ApiResponse<Media>.Ok(media));
    }
}
