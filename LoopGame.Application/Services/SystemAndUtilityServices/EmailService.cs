using LoopGame.Application.IServices.SystemAndUtilityServices;
using LoopGame.Infrastructure.Email;
using LoopGame.Infrastructure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Services.SystemAndUtilityServices
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }
        public async Task<Result> SendEmail(string email, string Code, string Purpose)
        {
            try
            {
                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort);
                client.EnableSsl = _settings.EnableSsl;
                client.Credentials = new NetworkCredential(_settings.SenderEmail, _settings.Password);

                var subject = Purpose switch
                {
                    "ResetPassword" => "ShiftOS — Reset Your Password",
                    _ => "ShiftOS — Verification Code"
                };
                var message = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = subject,
                    Body = $"Your verification code is:{Code}",
                    IsBodyHtml = true
                };
                message.To.Add(email);

                 await client.SendMailAsync(message);
                _logger.LogInformation("OTP sent to {Email}", email);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP to {Email}", email);
                return Result.Failure(new Error("EmailService.SendEmail", "Failed to send email."));
            }
        }
    }
}
