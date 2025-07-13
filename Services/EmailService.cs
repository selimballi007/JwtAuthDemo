using Microsoft.Extensions.Configuration;
using Resend;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly IResend _resend;

    public EmailService(IConfiguration config, IResend resend)
    {
        _config = config;
        _resend = resend;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var message = new EmailMessage();
        message.From = "onboarding@email.devexer.uk";
        message.To.Add(toEmail);
        message.Subject = subject;
        message.HtmlBody = htmlBody;

        await _resend.EmailSendAsync(message);
    }
}
