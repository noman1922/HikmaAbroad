using HikmaAbroad.Configuration;
using HikmaAbroad.Models.Entities;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HikmaAbroad.Services;

public interface IEmailService
{
    Task SendStudentSubmissionNotificationAsync(Student student);
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendStudentSubmissionNotificationAsync(Student student)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Email disabled. Skipping notification for student {Name}", student.Name);
            return;
        }

        try
        {
            var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? _settings.SmtpHost;
            var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? _settings.SmtpUser;
            var smtpPass = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? _settings.SmtpPassword;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(_settings.AdminNotificationEmail));
            message.Subject = $"New Student Submission: {student.Name}";

            message.Body = new TextPart("html")
            {
                Text = $@"
                <h2>New Student Application Submitted</h2>
                <p><strong>Name:</strong> {student.Name}</p>
                <p><strong>Email:</strong> {student.Email}</p>
                <p><strong>Phone:</strong> {student.Phone}</p>
                <p><strong>From Country:</strong> {student.FromCountry}</p>
                <p><strong>Academic Level:</strong> {student.LastAcademicLevel}</p>
                <p><strong>Submitted At:</strong> {student.UpdatedAt:yyyy-MM-dd HH:mm} UTC</p>
                "
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, _settings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Notification email sent for student {Name}", student.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification email for student {Name}", student.Name);
        }
    }
}
