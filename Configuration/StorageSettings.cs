namespace HikmaAbroad.Configuration;

public class StorageSettings
{
    public string Mode { get; set; } = "Local"; // "Local" or "S3"
    public string LocalPath { get; set; } = "wwwroot/uploads";
    public string BaseUrl { get; set; } = "/uploads";
    public S3Settings S3 { get; set; } = new();
}

public class S3Settings
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;
    public string CdnBaseUrl { get; set; } = string.Empty;
}
