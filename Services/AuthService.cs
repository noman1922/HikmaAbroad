using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HikmaAbroad.Configuration;
using HikmaAbroad.Data;
using HikmaAbroad.Models.DTOs;
using HikmaAbroad.Models.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

namespace HikmaAbroad.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task SeedAdminAsync();
}

public class AuthService : IAuthService
{
    private readonly MongoDbContext _db;
    private readonly JwtSettings _jwt;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(MongoDbContext db, IOptions<JwtSettings> jwt, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _jwt = jwt.Value;
        _config = config;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.AdminUsers
            .Find(u => u.Email == request.Email.ToLowerInvariant())
            .FirstOrDefaultAsync();

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return null;
        }

        var secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? _jwt.Secret;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id!),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        _logger.LogInformation("Admin {Email} logged in successfully", user.Email);

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expires,
            Name = user.Name,
            Email = user.Email
        };
    }

    public async Task SeedAdminAsync()
    {
        var seedEmail = _config["AdminSeed:Email"] ?? "admin@hikmaconsult.com";
        var seedPassword = _config["AdminSeed:Password"] ?? "Admin@123";

        var existing = await _db.AdminUsers.Find(u => u.Email == seedEmail.ToLowerInvariant()).FirstOrDefaultAsync();
        if (existing != null) return;

        var admin = new AdminUser
        {
            Email = seedEmail.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword),
            Name = "Admin",
            Role = "admin"
        };

        await _db.AdminUsers.InsertOneAsync(admin);
        _logger.LogInformation("Seed admin user created: {Email}", seedEmail);
    }
}
