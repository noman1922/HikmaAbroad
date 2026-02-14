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
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string CtaText { get; set; } = string.Empty;
    public string CtaUrl { get; set; } = string.Empty;
    public List<string> Banners { get; set; } = new();
}

public class NavItem
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class FooterSection
{
    public string Text { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
