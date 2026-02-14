using Amazon.S3;
using Amazon.S3.Model;
using HikmaAbroad.Configuration;
using HikmaAbroad.Data;
using HikmaAbroad.Models.Entities;
using Microsoft.Extensions.Options;

namespace HikmaAbroad.Services;

public interface IFileStorageService
{
    Task<Media> UploadAsync(Stream fileStream, string fileName, string contentType, long sizeBytes);
}

public class LocalFileStorageService : IFileStorageService
{
    private readonly StorageSettings _settings;
    private readonly MongoDbContext _db;
    private readonly IWebHostEnvironment _env;

    public LocalFileStorageService(IOptions<StorageSettings> settings, MongoDbContext db, IWebHostEnvironment env)
    {
        _settings = settings.Value;
        _db = db;
        _env = env;
    }

    public async Task<Media> UploadAsync(Stream fileStream, string fileName, string contentType, long sizeBytes)
    {
        var ext = Path.GetExtension(fileName);
        var storedName = $"{Guid.NewGuid()}{ext}";
        var uploadDir = Path.Combine(_env.ContentRootPath, _settings.LocalPath);
        Directory.CreateDirectory(uploadDir);

        var filePath = Path.Combine(uploadDir, storedName);
        using (var fs = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fs);
        }

        var url = $"{_settings.BaseUrl}/{storedName}";

        var media = new Media
        {
            FileName = fileName,
            StoredFileName = storedName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            Url = url
        };

        await _db.Media.InsertOneAsync(media);
        return media;
    }
}

public class S3FileStorageService : IFileStorageService
{
    private readonly StorageSettings _settings;
    private readonly MongoDbContext _db;
    private readonly ILogger<S3FileStorageService> _logger;

    public S3FileStorageService(IOptions<StorageSettings> settings, MongoDbContext db, ILogger<S3FileStorageService> logger)
    {
        _settings = settings.Value;
        _db = db;
        _logger = logger;
    }

    public async Task<Media> UploadAsync(Stream fileStream, string fileName, string contentType, long sizeBytes)
    {
        var ext = Path.GetExtension(fileName);
        var storedName = $"{Guid.NewGuid()}{ext}";

        var s3 = _settings.S3;
        var accessKey = Environment.GetEnvironmentVariable("S3_ACCESS_KEY") ?? s3.AccessKey;
        var secretKey = Environment.GetEnvironmentVariable("S3_SECRET_KEY") ?? s3.SecretKey;

        var config = new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(s3.Region) };
        if (!string.IsNullOrEmpty(s3.ServiceUrl))
            config.ServiceURL = s3.ServiceUrl;

        using var client = new AmazonS3Client(accessKey, secretKey, config);

        var putRequest = new PutObjectRequest
        {
            BucketName = s3.BucketName,
            Key = $"uploads/{storedName}",
            InputStream = fileStream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        };

        await client.PutObjectAsync(putRequest);

        var url = !string.IsNullOrEmpty(s3.CdnBaseUrl)
            ? $"{s3.CdnBaseUrl.TrimEnd('/')}/uploads/{storedName}"
            : $"https://{s3.BucketName}.s3.{s3.Region}.amazonaws.com/uploads/{storedName}";

        var media = new Media
        {
            FileName = fileName,
            StoredFileName = storedName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            Url = url
        };

        await _db.Media.InsertOneAsync(media);
        _logger.LogInformation("File uploaded to S3: {Url}", url);
        return media;
    }
}
