using HikmaAbroad.Configuration;
using HikmaAbroad.Models.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HikmaAbroad.Data;

/// <summary>
/// MongoDB database context providing typed collection accessors.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<SiteSettings> SiteSettings => _database.GetCollection<SiteSettings>("SiteSettings");
    public IMongoCollection<Destination> Destinations => _database.GetCollection<Destination>("Destinations");
    public IMongoCollection<StudentExperience> StudentExperiences => _database.GetCollection<StudentExperience>("StudentExperiences");
    public IMongoCollection<Service> Services => _database.GetCollection<Service>("Services");
    public IMongoCollection<Counsellor> Counsellors => _database.GetCollection<Counsellor>("Counsellors");
    public IMongoCollection<Page> Pages => _database.GetCollection<Page>("Pages");
    public IMongoCollection<Student> Students => _database.GetCollection<Student>("Students");
    public IMongoCollection<AdminUser> AdminUsers => _database.GetCollection<AdminUser>("AdminUsers");
    public IMongoCollection<Media> Media => _database.GetCollection<Media>("Media");

    /// <summary>
    /// Create indexes for performance.
    /// </summary>
    public async Task EnsureIndexesAsync()
    {
        // Students indexes
        var studentIndexes = Builders<Student>.IndexKeys;
        await Students.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Student>(studentIndexes.Ascending(s => s.IsSubmitted)),
            new CreateIndexModel<Student>(studentIndexes.Ascending(s => s.DraftToken)),
            new CreateIndexModel<Student>(studentIndexes.Descending(s => s.CreatedAt)),
        });

        // Destinations indexes
        var destIndexes = Builders<Destination>.IndexKeys;
        await Destinations.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Destination>(destIndexes.Ascending(d => d.Slug), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<Destination>(destIndexes.Ascending(d => d.IsActive)),
        });

        // Services index
        await Services.Indexes.CreateOneAsync(
            new CreateIndexModel<Service>(Builders<Service>.IndexKeys.Ascending(s => s.IsActive)));

        // Pages index
        await Pages.Indexes.CreateOneAsync(
            new CreateIndexModel<Page>(Builders<Page>.IndexKeys.Ascending(p => p.Key), new CreateIndexOptions { Unique = true }));

        // AdminUsers index
        await AdminUsers.Indexes.CreateOneAsync(
            new CreateIndexModel<AdminUser>(Builders<AdminUser>.IndexKeys.Ascending(a => a.Email), new CreateIndexOptions { Unique = true }));
    }
}
