namespace Hubbly.Application.Common.Models;

public class EmailSettings
{
    public string SmtpServer { get; set; } = "smtp.yandex.ru";
    public int SmtpPort { get; set; } = 465;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderPassword { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
}