using Hubbly.Domain.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Hubbly.Application.Common.Models;

namespace Hubbly.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        try
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Hubbly", _emailSettings.SenderEmail));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("html") { Text = message };

            using var client = new SmtpClient();

            var secureSocketOptions = _emailSettings.SmtpPort == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable;

            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, secureSocketOptions);
            await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.SenderPassword);

            await client.SendAsync(emailMessage);

            _logger.LogInformation("Email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", email);
            // Не бросаем исключение, чтобы не ломать регистрацию
        }
    }

    public async Task SendWelcomeEmailAsync(string email, string nickname)
    {
        var subject = "Добро пожаловать в Hubbly!";
        var message = $@"
            <h2>Привет, {nickname}!</h2>
            <p>Вы успешно зарегистрировались в Hubbly.</p>
            <p>Ваш email был привязан к аккаунту. Теперь вы можете:</p>
            <ul>
                <li>Создавать свои комнаты</li>
                <li>Приглашать друзей</li>
                <li>Настроить профиль</li>
            </ul>
            <p>Приятного общения!</p>
        ";

        await SendEmailAsync(email, subject, message);
    }
}