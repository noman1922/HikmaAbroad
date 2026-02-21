using System.Text;
using AspNetCoreRateLimit;
using HikmaAbroad.Configuration;
using HikmaAbroad.Data;
using HikmaAbroad.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using HikmaAbroad.Models.Entities;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// ── Configuration bindings ───────────────────────────────────────────
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));

// Override from env vars
var mongoUri = Environment.GetEnvironmentVariable("MONGO_URI");
if (!string.IsNullOrEmpty(mongoUri))
    builder.Configuration["MongoDb:ConnectionString"] = mongoUri;

// ── MongoDB context ──────────────────────────────────────────────────
builder.Services.AddSingleton<MongoDbContext>();

// ── Services ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddTransient<SeedDataService>();
builder.Services.AddHttpClient();

// File storage (Local or S3 based on config)
var storageMode = builder.Configuration["Storage:Mode"] ?? "Local";
if (storageMode.Equals("S3", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<IFileStorageService, S3FileStorageService>();
else
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// ── JWT Authentication ───────────────────────────────────────────────
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["Jwt:Secret"]
    ?? "CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_KEY_AT_LEAST_32_CHARS";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// ── CORS ─────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── Rate Limiting ────────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddInMemoryRateLimiting();

// ── Controllers & Swagger ────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hikma Abroad API",
        Version = "v1",
        Description = "REST API for Hikma Consult study-abroad platform. Public endpoints for home data and student submissions. Admin endpoints for content management.",
        Contact = new OpenApiContact { Name = "Hikma Consult", Email = "dev@hikmaconsult.com" }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);

    c.EnableAnnotations();
});

// ── Response compression ─────────────────────────────────────────────
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// ── Ensure indexes and seed data ─────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<SeedDataService>();
    await seeder.SeedAsync();

    // ── Data Migration / Integrity Fix ───────────────────────────────────
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        
        // Fix Universities (Ensure consistency)
        var unisToFix = await db.Universities.Find(u => true).ToListAsync();
        // (Logic here if needed, but BsonElement handles most)

        // Fix Courses (UniversityName)
        var coursesToFix = await db.Courses.Find(c => string.IsNullOrEmpty(c.UniversityName)).ToListAsync();
        foreach (var course in coursesToFix)
        {
            if (!string.IsNullOrEmpty(course.UniversityId))
            {
                var uni = await db.Universities.Find(u => u.Id == course.UniversityId).FirstOrDefaultAsync();
                if (uni != null)
                {
                    await db.Courses.UpdateOneAsync(
                        c => c.Id == course.Id,
                        Builders<Course>.Update.Set("universityName", uni.Name)
                    );
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration warning: {ex.Message}");
    }
}

// ── Middleware pipeline ──────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseResponseCompression();
app.UseSerilogRequestLogging();
app.UseIpRateLimiting();
app.UseCors();
app.UseStaticFiles(); // serve wwwroot/uploads

app.UseAuthentication();
app.UseAuthorization();

// Swagger always available (can restrict in prod if needed)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hikma Abroad API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();

// Print information to console
if (app.Environment.IsDevelopment())
{
    var appUrl = app.Urls.FirstOrDefault() ?? "http://localhost:5076";
    Console.WriteLine("-------------------------------------------------------");
    Console.WriteLine($"Hikma Abroad API is running in DEVELOPMENT mode");
    Console.WriteLine($"Swagger UI: {appUrl}/swagger");
    Console.WriteLine("-------------------------------------------------------");
}

app.Run();
