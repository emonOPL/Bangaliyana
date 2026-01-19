using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Bangaliyana.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ISiteSettingsService _siteSettingsService;

        public EmailService(IConfiguration config, ILogger<EmailService> logger, IHostEnvironment hostEnvironment, ISiteSettingsService siteSettingsService)
        {
            _config = config;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _siteSettingsService = siteSettingsService;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpHost = _config["Smtp:Host"];
                var smtpPortString = _config["Smtp:Port"];
                var smtpUser = _config["Smtp:User"];
                var smtpPass = _config["Smtp:Pass"];

                // Check if SMTP is properly configured
                bool isConfigured = !string.IsNullOrEmpty(smtpHost) &&
                                    !string.IsNullOrEmpty(smtpPortString) &&
                                    !string.IsNullOrEmpty(smtpUser) &&
                                    !string.IsNullOrEmpty(smtpPass) &&
                                    smtpUser != "your-email@gmail.com"; // Check for placeholder

                if (!isConfigured)
                {
                    if (_hostEnvironment.IsDevelopment())
                    {
                        // In development, log basic info only (no sensitive content)
                        _logger.LogInformation("Email to {ToEmail} - Subject: {Subject} (SMTP not configured, Development Mode)", toEmail, subject);
                        return;
                    }
                    else
                    {
                        _logger.LogError("SMTP is not configured. Email to {ToEmail} not sent", toEmail);
                        return;
                    }
                }

                using var smtpClient = new SmtpClient(smtpHost)
                {
                    Port = int.Parse(smtpPortString!),
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                // Get site name from settings for email From name
                var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
                var emailFromName = siteSettings?.SiteName ?? "Bangaliyana";

                using var mail = new MailMessage
                {
                    From = new MailAddress(smtpUser!, emailFromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mail.To.Add(toEmail);

                await smtpClient.SendMailAsync(mail);
                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            }
        }
    }
}
