using HikmaAbroad.Data;
using HikmaAbroad.Models;
using HikmaAbroad.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HikmaAbroad.Controllers;

/// <summary>
/// Public home page data endpoint.
/// </summary>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class HomeController : ControllerBase
{
    private readonly MongoDbContext _db;

    public HomeController(MongoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns aggregated home page data: hero, navbar, footer, experiences, destinations, services, counsellors, about summary.
    /// </summary>
    [HttpGet("home")]
    [ResponseCache(Duration = 60)]
    [ProducesResponseType(typeof(ApiResponse<HomePageResponse>), 200)]
    public async Task<IActionResult> GetHome()
    {
        var settings = await _db.SiteSettings.Find(s => s.Id == "settings").FirstOrDefaultAsync();
        var destinations = await _db.Destinations.Find(d => d.IsActive).ToListAsync();
        var services = await _db.Services.Find(s => s.IsActive).ToListAsync();
        var counsellors = await _db.Counsellors.Find(_ => true).ToListAsync();
        var experiences = await _db.StudentExperiences.Find(_ => true).SortBy(e => e.DisplayOrder).ToListAsync();
        var aboutPage = await _db.Pages.Find(p => p.Key == "about").FirstOrDefaultAsync();

        var response = new HomePageResponse
        {
            Hero = settings?.Hero,
            Navbar = settings?.Navbar,
            Footer = settings?.Footer,
            Destinations = destinations,
            Services = services,
            Counsellors = counsellors,
            StudentExperiences = experiences,
            AboutSummary = aboutPage != null ? new { aboutPage.Title, aboutPage.HtmlContent } : null
        };

        return Ok(ApiResponse<HomePageResponse>.Ok(response));
    }
}
