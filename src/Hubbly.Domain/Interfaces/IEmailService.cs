namespace Hubbly.Domain.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string email, string subject, string message);
    Task SendWelcomeEmailAsync(string email, string nickname);
}