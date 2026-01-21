using Hubbly.Application.Common.Interfaces;
using Hubbly.Application.Common.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Hubbly.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
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
        await client.DisconnectAsync(true);
    }

    public async Task SendOtpEmailAsync(string email, string otpCode)
    {
        var subject = "Ваш код подтверждения Hubbly";
        var message = $@"
            <h2>Добро пожаловать в Hubbly!</h2>
            <p>Ваш код подтверждения: <strong>{otpCode}</strong></p>
            <p>Код действителен в течение 10 минут.</p>
            <p>Если вы не регистрировались в Hubbly, просто проигнорируйте это письмо.</p>
        ";

        await SendEmailAsync(email, subject, message);
    }
}