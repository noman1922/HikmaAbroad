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
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CreateOrUpdate([FromBody] StudentRequest request)
    {
        var source = request.Source?.ToLowerInvariant() ?? "application";
        
        // Validation for submitted forms
        if (request.IsSubmitted)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 2)
                return BadRequest(ApiResponse<object>.Fail("Name is required (min 2 characters) for submission"));

            if (!string.IsNullOrEmpty(request.Email) && !IsValidEmail(request.Email))
                return BadRequest(ApiResponse<object>.Fail("Invalid email format"));
        }

        // Sanitize common inputs
        var name = ValidationHelper.Sanitize(request.Name);
        var email = ValidationHelper.Sanitize(request.Email)?.ToLowerInvariant() ?? string.Empty;
        var phone = ValidationHelper.NormalizePhone(request.Phone);

        if (source == "contact")
        {
            return await HandleContactPayload(request, name, email, phone);
        }
        else
        {
            return await HandleStudentPayload(request, name, email, phone);
        }
    }

    private async Task<IActionResult> HandleStudentPayload(StudentRequest request, string name, string email, string phone)
    {
        var fromCountry = ValidationHelper.Sanitize(request.FromCountry);
        var academicLevel = ValidationHelper.Sanitize(request.LastAcademicLevel);

        Student? student = null;

        if (!string.IsNullOrEmpty(request.DraftToken))
        {
            student = await _db.Students
                .Find(s => s.DraftToken == request.DraftToken && !s.IsSubmitted)
                .FirstOrDefaultAsync();
        }

        if (student != null)
        {
            var update = Builders<Student>.Update
                .Set(s => s.Name, name)
                .Set(s => s.Email, email)
                .Set(s => s.Phone, phone)
                .Set(s => s.FromCountry, fromCountry)
                .Set(s => s.LastAcademicLevel, academicLevel)
                .Set(s => s.Purpose, request.Purpose)
                .Set(s => s.UniversityId, request.UniversityId)
                .Set(s => s.CourseId, request.CourseId)
                .Set(s => s.IsSubmitted, request.IsSubmitted)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await _db.Students.UpdateOneAsync(s => s.Id == student.Id, update);
            student = await _db.Students.Find(s => s.Id == student.Id).FirstOrDefaultAsync();
        }
        else
        {
            var draftToken = request.DraftToken ?? Guid.NewGuid().ToString("N");
            student = new Student
            {
                Name = name,
                Email = email,
                Phone = phone,
                FromCountry = fromCountry,
                LastAcademicLevel = academicLevel,
                Purpose = request.Purpose,
                UniversityId = request.UniversityId,
                CourseId = request.CourseId,
                IsSubmitted = request.IsSubmitted,
                DraftToken = draftToken,
                Source = "application",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _db.Students.InsertOneAsync(student);
        }

        if (request.IsSubmitted && student != null)
        {
            _ = Task.Run(async () =>
            {
                await _email.SendStudentSubmissionNotificationAsync(student);
                await _webhook.TriggerStudentSubmittedAsync(student);
            });
        }

        _logger.LogInformation("Student {Action}: {Id}. DraftToken: {Token}", 
            request.IsSubmitted ? "submitted" : "draft saved", 
            student?.Id, 
            student?.DraftToken);
            
        return Ok(ApiResponse<Student>.Ok(student!));
    }

    private async Task<IActionResult> HandleContactPayload(StudentRequest request, string name, string email, string phone)
    {
        var subject = ValidationHelper.Sanitize(request.Subject);
        var message = ValidationHelper.Sanitize(request.Message);

        Contact? contact = null;

        if (!string.IsNullOrEmpty(request.DraftToken))
        {
            contact = await _db.Contacts
                .Find(c => c.DraftToken == request.DraftToken && !c.IsSubmitted)
                .FirstOrDefaultAsync();
        }

        if (contact != null)
        {
            var update = Builders<Contact>.Update
                .Set(c => c.Name, name)
                .Set(c => c.Email, email)
                .Set(c => c.Phone, phone)
                .Set(c => c.Subject, subject ?? string.Empty)
                .Set(c => c.Message, message ?? string.Empty)
                .Set(c => c.IsSubmitted, request.IsSubmitted)
                .Set(c => c.UpdatedAt, DateTime.UtcNow);

            await _db.Contacts.UpdateOneAsync(c => c.Id == contact.Id, update);
            contact = await _db.Contacts.Find(c => c.Id == contact.Id).FirstOrDefaultAsync();
        }
        else
        {
            var draftToken = request.DraftToken ?? Guid.NewGuid().ToString("N");
            contact = new Contact
            {
                Name = name,
                Email = email,
                Phone = phone,
                Subject = subject ?? string.Empty,
                Message = message ?? string.Empty,
                IsSubmitted = request.IsSubmitted,
                DraftToken = draftToken,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _db.Contacts.InsertOneAsync(contact);
        }

        if (request.IsSubmitted && contact != null)
        {
            // Optional: send different email for contact
            _logger.LogInformation("Contact message submitted: {Id}", contact.Id);
        }

        _logger.LogInformation("Contact {Action}: {Id}. DraftToken: {Token}", 
            request.IsSubmitted ? "submitted" : "draft saved", 
            contact?.Id, 
            contact?.DraftToken);
        return Ok(ApiResponse<Contact>.Ok(contact!));
    }

    /// <summary>
    /// Get a student draft by draftToken (public).
    /// </summary>
    [HttpGet("draft/{draftToken}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDraft(string draftToken)
    {
        // Check students first
        var student = await _db.Students
            .Find(s => s.DraftToken == draftToken && !s.IsSubmitted)
            .FirstOrDefaultAsync();

        if (student != null) return Ok(ApiResponse<Student>.Ok(student));

        // Check contacts
        var contact = await _db.Contacts
            .Find(c => c.DraftToken == draftToken && !c.IsSubmitted)
            .FirstOrDefaultAsync();

        if (contact != null) return Ok(ApiResponse<Contact>.Ok(contact));

        return NotFound(ApiResponse<object>.Fail("Draft not found"));
    }

    /// <summary>
    /// List students with filters and paging (admin).
    /// </summary>
    /// <param name="isSubmitted">Filter by submission status</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (max 100)</param>
    [HttpGet]
    [Authorize(Roles = "admin,agent")]
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
    [Authorize(Roles = "admin,agent")]
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
    [Authorize(Roles = "admin,agent")]
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
    /// Update student application status (admin).
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<Student>), 200)]
    public async Task<IActionResult> UpdateStudentStatus(string id, [FromBody] StatusUpdateRequest request)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest(ApiResponse<object>.Fail("ID is required"));

        var update = Builders<Student>.Update.Set(s => s.UpdatedAt, DateTime.UtcNow);
        bool hasUpdate = false;

        if (request.IsRead.HasValue) 
        {
            update = update.Set(s => s.IsRead, request.IsRead.Value);
            hasUpdate = true;
        }
        if (request.IsDone.HasValue)
        {
            update = update.Set(s => s.IsDone, request.IsDone.Value);
            hasUpdate = true;
        }

        if (!hasUpdate) return Ok(ApiResponse<object>.Ok(null!, "No changes requested"));

        var result = await _db.Students.UpdateOneAsync(s => s.Id == id, update);
        if (result.MatchedCount == 0) return NotFound(ApiResponse<object>.Fail("Student application not found"));

        var student = await _db.Students.Find(s => s.Id == id).FirstOrDefaultAsync();
        return Ok(ApiResponse<Student>.Ok(student!));
    }

    /// <summary>
    /// Update contact request status (admin).
    /// </summary>
    [HttpPatch("contacts/{id}/status")]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(ApiResponse<Contact>), 200)]
    public async Task<IActionResult> UpdateContactStatus(string id, [FromBody] StatusUpdateRequest request)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest(ApiResponse<object>.Fail("ID is required"));

        var update = Builders<Contact>.Update.Set(c => c.UpdatedAt, DateTime.UtcNow);
        bool hasUpdate = false;

        if (request.IsRead.HasValue)
        {
            update = update.Set(c => c.IsRead, request.IsRead.Value);
            hasUpdate = true;
        }
        if (request.IsDone.HasValue)
        {
            update = update.Set(c => c.IsDone, request.IsDone.Value);
            hasUpdate = true;
        }

        if (!hasUpdate) return Ok(ApiResponse<object>.Ok(null!, "No changes requested"));

        var result = await _db.Contacts.UpdateOneAsync(c => c.Id == id, update);
        if (result.MatchedCount == 0) return NotFound(ApiResponse<object>.Fail("Contact record not found"));

        var contact = await _db.Contacts.Find(c => c.Id == id).FirstOrDefaultAsync();
        return Ok(ApiResponse<Contact>.Ok(contact!));
    }

    /// <summary>
    /// Cleanup old drafts (admin). Deletes drafts older than configured TTL.
    /// </summary>
    [HttpDelete("drafts/cleanup")]
    [Authorize(Roles = "admin,agent")]
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

    /// <summary>
    /// List contact requests with paging (admin/agent).
    /// </summary>
    [HttpGet("contacts")]
    [Authorize(Roles = "admin,agent")]
    [ProducesResponseType(typeof(PagedResponse<Contact>), 200)]
    public async Task<IActionResult> ListContacts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var filter = Builders<Contact>.Filter.Eq(c => c.IsSubmitted, true);
        var totalCount = await _db.Contacts.CountDocumentsAsync(filter);
        var contacts = await _db.Contacts
            .Find(filter)
            .SortByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return Ok(new PagedResponse<Contact>
        {
            Data = contacts,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
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
