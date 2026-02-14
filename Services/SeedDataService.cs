using HikmaAbroad.Data;
using HikmaAbroad.Models.Entities;
using MongoDB.Driver;

namespace HikmaAbroad.Services;

public class SeedDataService
{
    private readonly MongoDbContext _db;
    private readonly IAuthService _auth;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(MongoDbContext db, IAuthService auth, ILogger<SeedDataService> logger)
    {
        _db = db;
        _auth = auth;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await _auth.SeedAdminAsync();
        await SeedSiteSettingsAsync();
        await SeedDestinationsAsync();
        await SeedServicesAsync();
        await SeedCounsellorsAsync();
        await SeedExperiencesAsync();
        await SeedPagesAsync();
        _logger.LogInformation("Seed data completed");
    }

    private async Task SeedSiteSettingsAsync()
    {
        var existing = await _db.SiteSettings.Find(s => s.Id == "settings").FirstOrDefaultAsync();
        if (existing != null) return;

        await _db.SiteSettings.InsertOneAsync(new SiteSettings
        {
            Id = "settings",
            SiteName = "Hikma Consult",
            LogoUrl = "/uploads/logo.png",
            Hero = new HeroSection
            {
                Title = "Your Gateway to Study Abroad",
                Subtitle = "Expert guidance for international education",
                CtaText = "Apply Now",
                CtaUrl = "/apply",
                Banners = new List<string> { "/uploads/banner1.jpg", "/uploads/banner2.jpg" }
            },
            Navbar = new List<NavItem>
            {
                new() { Title = "Home", Url = "/" },
                new() { Title = "Destinations", Url = "/destinations" },
                new() { Title = "Services", Url = "/services" },
                new() { Title = "About", Url = "/about" },
                new() { Title = "Contact", Url = "/contact" }
            },
            Footer = new FooterSection
            {
                Text = "© 2026 Hikma Consult. All rights reserved.",
                Address = "123 Education Street, Dhaka, Bangladesh",
                Phone = "+8801700000000",
                Email = "info@hikmaconsult.com"
            }
        });
        _logger.LogInformation("Seeded SiteSettings");
    }

    private async Task SeedDestinationsAsync()
    {
        if (await _db.Destinations.CountDocumentsAsync(_ => true) > 0) return;

        var destinations = new List<Destination>
        {
            new() { Country = "Malaysia", Slug = "malaysia", IsActive = true, Description = "Affordable quality education in Southeast Asia", ImageUrl = "/uploads/malaysia.jpg" },
            new() { Country = "United Kingdom", Slug = "united-kingdom", IsActive = true, Description = "World-class universities and rich culture", ImageUrl = "/uploads/uk.jpg" },
            new() { Country = "Canada", Slug = "canada", IsActive = true, Description = "Welcoming environment with top-ranked institutions", ImageUrl = "/uploads/canada.jpg" },
            new() { Country = "Australia", Slug = "australia", IsActive = true, Description = "Innovative education and vibrant student life", ImageUrl = "/uploads/australia.jpg" },
        };

        await _db.Destinations.InsertManyAsync(destinations);
        _logger.LogInformation("Seeded {Count} destinations", destinations.Count);
    }

    private async Task SeedServicesAsync()
    {
        if (await _db.Services.CountDocumentsAsync(_ => true) > 0) return;

        var services = new List<Service>
        {
            new() { Title = "University Application", Description = "Complete assistance with university applications", IsActive = true },
            new() { Title = "Visa Processing", Description = "Expert visa guidance and documentation support", IsActive = true },
            new() { Title = "Scholarship Guidance", Description = "Find and apply for scholarships worldwide", IsActive = true },
            new() { Title = "Pre-Departure Briefing", Description = "Everything you need to know before you fly", IsActive = true },
        };

        await _db.Services.InsertManyAsync(services);
        _logger.LogInformation("Seeded {Count} services", services.Count);
    }

    private async Task SeedCounsellorsAsync()
    {
        if (await _db.Counsellors.CountDocumentsAsync(_ => true) > 0) return;

        var counsellors = new List<Counsellor>
        {
            new() { Name = "Riyad Ahmed", Title = "Senior Counselor", Bio = "10+ years of experience in international education consulting", PhotoUrl = "/uploads/riyad.jpg", Contact = "riyad@hikmaconsult.com" },
            new() { Name = "Fatima Khan", Title = "Visa Specialist", Bio = "Expert in visa processing for UK, Canada, and Australia", PhotoUrl = "/uploads/fatima.jpg", Contact = "fatima@hikmaconsult.com" },
        };

        await _db.Counsellors.InsertManyAsync(counsellors);
        _logger.LogInformation("Seeded {Count} counsellors", counsellors.Count);
    }

    private async Task SeedExperiencesAsync()
    {
        if (await _db.StudentExperiences.CountDocumentsAsync(_ => true) > 0) return;

        var experiences = new List<StudentExperience>
        {
            new() { Name = "Ahsan Rahman", ShortText = "Studying in Malaysia was the best decision!", FullText = "Hikma Consult helped me every step of the way. From choosing the right university to getting my visa approved, they were always there.", ImageUrl = "/uploads/ahsan.jpg", DisplayOrder = 1 },
            new() { Name = "Nadia Islam", ShortText = "Got a scholarship to study in the UK!", FullText = "Thanks to the scholarship guidance from Hikma Consult, I received a partial scholarship at a top UK university.", ImageUrl = "/uploads/nadia.jpg", DisplayOrder = 2 },
        };

        await _db.StudentExperiences.InsertManyAsync(experiences);
        _logger.LogInformation("Seeded {Count} student experiences", experiences.Count);
    }

    private async Task SeedPagesAsync()
    {
        if (await _db.Pages.CountDocumentsAsync(_ => true) > 0) return;

        await _db.Pages.InsertOneAsync(new Page
        {
            Key = "about",
            Title = "About Us",
            HtmlContent = "<p>Hikma Consult is a leading education consultancy helping students achieve their dreams of studying abroad. With years of experience and a dedicated team, we provide comprehensive support from application to arrival.</p>"
        });
        _logger.LogInformation("Seeded pages");
    }
}
