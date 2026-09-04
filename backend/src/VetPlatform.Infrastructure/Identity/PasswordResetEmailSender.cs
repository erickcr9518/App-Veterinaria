using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Infrastructure.Identity;

public class PasswordResetEmailSender : IPasswordResetEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PasswordResetEmailSender> _logger;

    public PasswordResetEmailSender(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<PasswordResetEmailSender> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(
        string email,
        string fullName,
        string resetUrl,
        CancellationToken cancellationToken)
    {
        var host = _configuration["PasswordReset:Smtp:Host"];
        var fromEmail = _configuration["PasswordReset:Smtp:FromEmail"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
        {
            LogMissingSmtp(email, resetUrl);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(
                fromEmail,
                _configuration["PasswordReset:Smtp:FromName"] ?? "VetPlatform"),
            Subject = "Restablece tu contrasena de VetPlatform",
            Body = $"""
Hola {fullName},

Recibimos una solicitud para restablecer tu contrasena.

Abre este enlace para crear una contrasena nueva:
{resetUrl}

Si no solicitaste este cambio, puedes ignorar este mensaje.
""",
        };
        message.To.Add(email);

        using var client = new SmtpClient(host, _configuration.GetValue("PasswordReset:Smtp:Port", 587))
        {
            EnableSsl = _configuration.GetValue("PasswordReset:Smtp:EnableSsl", true),
        };

        var userName = _configuration["PasswordReset:Smtp:UserName"];
        var password = _configuration["PasswordReset:Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(userName))
        {
            client.Credentials = new NetworkCredential(userName, password);
        }

        using var registration = cancellationToken.Register(client.SendAsyncCancel);
        await client.SendMailAsync(message, cancellationToken);
    }

    private void LogMissingSmtp(string email, string resetUrl)
    {
        if (_environment.IsProduction())
        {
            _logger.LogWarning(
                "Password reset requested for {Email}, but SMTP is not configured.",
                email);
            return;
        }

        _logger.LogInformation(
            "Password reset requested for {Email}. SMTP is not configured; local reset URL: {ResetUrl}",
            email,
            resetUrl);
    }
}
