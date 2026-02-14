using System.Globalization;
using CsvHelper;
using HikmaAbroad.Data;
using HikmaAbroad.Helpers;
using HikmaAbroad.Models;
using HikmaAbroad.Models.DTOs;
using HikmaAbroad.Models.Entities;
using HikmaAbroad.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HikmaAbroad.Controllers;

/// <summary>
/// Student applications: submit, draft, admin list, export.
/// </summary>
[ApiController]
[Route("api/v1/students")]
[Produces("application/json")]
public class StudentsController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly IEmailService _email;
    private readonly IWebhookService _webhook;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(MongoDbContext db, IEmailService email, IWebhookService webhook, ILogger<StudentsController> logger)
    {
        _db = db;
        _email = email;
        _webhook = webhook;
        _logger = logger;
    }

    /// <summary>
    /// Submit or save a student application draft.
    /// If draftToken matches an existing draft, it updates it.
    /// If isSubmitted=true, marks as submitted and triggers notifications.
    /// </summary>
    /// <param name="request">Student application data</param>
    /// <returns>Saved student document with draftToken</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Student>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CreateOrUpdate([FromBody] StudentRequest request)
    {
        // Validation for submitted forms
        if (request.IsSubmitted)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 2)
                return BadRequest(ApiResponse<object>.Fail("Name is required (min 2 characters) for submission"));

            if (!string.IsNullOrEmpty(request.Email) && !IsValidEmail(request.Email))
                return BadRequest(ApiResponse<object>.Fail("Invalid email format"));
        }

        // Sanitize inputs
        var name = ValidationHelper.Sanitize(request.Name);
        var email = ValidationHelper.Sanitize(request.Email)?.ToLowerInvariant() ?? string.Empty;
        var phone = ValidationHelper.NormalizePhone(request.Phone);
        var fromCountry = ValidationHelper.Sanitize(request.FromCountry);
        var academicLevel = ValidationHelper.Sanitize(request.LastAcademicLevel);

        Student? student = null;

        // Check for existing draft by token
        if (!string.IsNullOrEmpty(request.DraftToken))
        {
            student = await _db.Students
                .Find(s => s.DraftToken == request.DraftToken && !s.IsSubmitted)
                .FirstOrDefaultAsync();
        }

        if (student != null)
        {
            // Update existing draft
            var update = Builders<Student>.Update
                .Set(s => s.Name, name)
                .Set(s => s.Email, email)
                .Set(s => s.Phone, phone)
                .Set(s => s.FromCountry, fromCountry)
                .Set(s => s.LastAcademicLevel, academicLevel)
                .Set(s => s.IsSubmitted, request.IsSubmitted)
                .Set(s => s.Source, request.Source ?? "web")
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await _db.Students.UpdateOneAsync(s => s.Id == student.Id, update);
            student = await _db.Students.Find(s => s.Id == student.Id).FirstOrDefaultAsync();
        }
        else
        {
            // Create new
            var draftToken = request.DraftToken ?? Guid.NewGuid().ToString("N");
            student = new Student
            {
                Name = name,
                Email = email,
                Phone = phone,
                FromCountry = fromCountry,
                LastAcademicLevel = academicLevel,
                IsSubmitted = request.IsSubmitted,
                DraftToken = draftToken,
                Source = request.Source ?? "web",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _db.Students.InsertOneAsync(student);
        }

        // Trigger notifications on submission
        if (request.IsSubmitted && student != null)
        {
            _ = Task.Run(async () =>
            {
                await _email.SendStudentSubmissionNotificationAsync(student);
                await _webhook.TriggerStudentSubmittedAsync(student);
            });
        }

        _logger.LogInformation("Student {Action}: {Id}", request.IsSubmitted ? "submitted" : "draft saved", student?.Id);
        return Ok(ApiResponse<Student>.Ok(student!));
    }

    /// <summary>
    /// Get a student draft by draftToken (public).
    /// </summary>
    [HttpGet("draft/{draftToken}")]
    [ProducesResponseType(typeof(ApiResponse<Student>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDraft(string draftToken)
    {
        var student = await _db.Students
            .Find(s => s.DraftToken == draftToken && !s.IsSubmitted)
            .FirstOrDefaultAsync();

        if (student == null) return NotFound(ApiResponse<object>.Fail("Draft not found"));
        return Ok(ApiResponse<Student>.Ok(student));
    }

    /// <summary>
    /// List students with filters and paging (admin).
    /// </summary>
    /// <param name="isSubmitted">Filter by submission status</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (max 100)</param>
    [HttpGet]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(PagedResponse<Student>), 200)]
    public async Task<IActionResult> List([FromQuery] bool? isSubmitted, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var filter = Builders<Student>.Filter.Empty;
        if (isSubmitted.HasValue)
            filter = Builders<Student>.Filter.Eq(s => s.IsSubmitted, isSubmitted.Value);

        var totalCount = await _db.Students.CountDocumentsAsync(filter);
        var students = await _db.Students
            .Find(filter)
            .SortByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return Ok(new PagedResponse<Student>
        {
            Data = students,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Get a student by ID (admin).
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<Student>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id)
    {
        var student = await _db.Students.Find(s => s.Id == id).FirstOrDefaultAsync();
        if (student == null) return NotFound(ApiResponse<object>.Fail("Student not found"));
        return Ok(ApiResponse<Student>.Ok(student));
    }

    /// <summary>
    /// Mark a student as contacted (admin).
    /// </summary>
    [HttpPut("{id}/mark-contacted")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<Student>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> MarkContacted(string id)
    {
        var update = Builders<Student>.Update
            .Set(s => s.IsContacted, true)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        var result = await _db.Students.UpdateOneAsync(s => s.Id == id, update);
        if (result.MatchedCount == 0) return NotFound(ApiResponse<object>.Fail("Student not found"));

        var student = await _db.Students.Find(s => s.Id == id).FirstOrDefaultAsync();
        return Ok(ApiResponse<Student>.Ok(student!));
    }

    /// <summary>
    /// Cleanup old drafts (admin). Deletes drafts older than configured TTL.
    /// </summary>
    [HttpDelete("drafts/cleanup")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> CleanupDrafts([FromServices] IConfiguration config)
    {
        var ttlDays = config.GetValue<int>("DraftCleanup:TTLDays", 30);
        var cutoff = DateTime.UtcNow.AddDays(-ttlDays);

        var result = await _db.Students.DeleteManyAsync(
            s => !s.IsSubmitted && s.CreatedAt < cutoff);

        _logger.LogInformation("Cleaned up {Count} old drafts", result.DeletedCount);
        return Ok(ApiResponse<object>.Ok(new { deletedCount = result.DeletedCount }));
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
