using System.Text;
using System.Text.Json;
using HikmaAbroad.Models.Entities;

namespace HikmaAbroad.Services;

public interface IWebhookService
{
    Task TriggerStudentSubmittedAsync(Student student);
}

public class WebhookService : IWebhookService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<WebhookService> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task TriggerStudentSubmittedAsync(Student student)
    {
        var enabled = _config.GetValue<bool>("Webhook:Enabled");
        var url = _config["Webhook:Url"];

        if (!enabled || string.IsNullOrEmpty(url)) return;

        try
        {
            var client = _httpClientFactory.CreateClient();
            var payload = JsonSerializer.Serialize(new
            {
                @event = "student.submitted",
                data = new
                {
                    id = student.Id,
                    name = student.Name,
                    email = student.Email,
                    phone = student.Phone,
                    fromCountry = student.FromCountry,
                    submittedAt = student.UpdatedAt
                }
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);

            _logger.LogInformation("Webhook triggered: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger webhook for student {Id}", student.Id);
        }
    }
}
