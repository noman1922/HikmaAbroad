using System.Globalization;
using CsvHelper;
using HikmaAbroad.Data;
using HikmaAbroad.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HikmaAbroad.Controllers;

/// <summary>
/// Data export endpoints (admin).
/// </summary>
[ApiController]
[Route("api/v1/export")]
[Authorize(Roles = "admin")]
public class ExportController : ControllerBase
{
    private readonly MongoDbContext _db;

    public ExportController(MongoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Export students as CSV. Optionally filter by isSubmitted.
    /// </summary>
    /// <param name="isSubmitted">Filter by submission status</param>
    [HttpGet("students")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileResult), 200)]
    public async Task<IActionResult> ExportStudents([FromQuery] bool? isSubmitted)
    {
        var filter = Builders<Student>.Filter.Empty;
        if (isSubmitted.HasValue)
            filter = Builders<Student>.Filter.Eq(s => s.IsSubmitted, isSubmitted.Value);

        var students = await _db.Students.Find(filter).SortByDescending(s => s.CreatedAt).ToListAsync();

        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(students.Select(s => new
            {
                s.Id,
                s.Name,
                s.Email,
                s.Phone,
                s.FromCountry,
                s.LastAcademicLevel,
                s.IsSubmitted,
                s.IsContacted,
                s.Source,
                s.DraftToken,
                CreatedAt = s.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedAt = s.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            }));
        }

        memoryStream.Position = 0;
        return File(memoryStream.ToArray(), "text/csv", $"students-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
