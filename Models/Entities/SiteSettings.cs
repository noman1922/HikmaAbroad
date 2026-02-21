using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HikmaAbroad.Models.Entities;

/// <summary>
/// Site-wide settings including hero, navbar, and footer configuration.
/// </summary>
public class SiteSettings
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = "settings";

    /// <summary>Site display name</summary>
    public string SiteName { get; set; } = "Hikma Consult";

    /// <summary>URL to the site logo</summary>
    public string LogoUrl { get; set; } = string.Empty;

    /// <summary>Hero section configuration</summary>
    public HeroSection Hero { get; set; } = new();

    /// <summary>Navigation bar links</summary>
    public List<NavItem> Navbar { get; set; } = new();

    /// <summary>Footer configuration</summary>
    public FooterSection Footer { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class HeroSection
{
    public string Title { get; set; } = "Expert Guidance For International Students In Malaysia";
    public string Subtitle { get; set; } = "We simplify everything—from choosing the perfect university to obtaining your student visa. Your dream education in Malaysia starts here.";
    public string CtaText { get; set; } = "Explore Universities";
    public string CtaUrl { get; set; } = "/universities";
    public List<string> Banners { get; set; } = new() { "https://images.unsplash.com/photo-1592280771190-3e2e4d571952?q=80&w=1974&auto=format&fit=crop" };
}

public class NavItem
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class FooterSection
{
    public string Text { get; set; } = "Your trusted partner for education in Malaysia.";
    public string Address { get; set; } = "Kuala Lumpur, Malaysia";
    public string Phone { get; set; } = "+60 123 456 789";
    public string Email { get; set; } = "hello@hikmahabroad.com";
}
