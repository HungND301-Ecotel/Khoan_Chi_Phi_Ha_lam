using Application.Configurations;
using Application.Interfaces.Infrastructures.Integrates.External.Service.Email;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace External.Service.Email;

public class EmailSender(IOptions<MailServerConfiguration> configuration) : IEmailSender
{
    private readonly MailServerConfiguration _settings = configuration.Value;

    public void Send(string from, string to, string subject, string html)
    {
        throw new NotImplementedException();
    }

    public async Task SendAsync(string to, string subject, string html)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_settings.ConnectionInfo.SenderAddress));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Html) { Text = html };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settings.ConnectionInfo.SmtpServer, _settings.ConnectionInfo.Port);
        await smtp.AuthenticateAsync(_settings.ConnectionInfo.Username, _settings.ConnectionInfo.Password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}