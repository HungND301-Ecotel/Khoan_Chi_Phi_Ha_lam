namespace Application.Interfaces.Infrastructures.Integrates.External.Service.Email;

public interface IEmailSender
{
    void Send(string from, string to, string subject, string html);

    Task SendAsync(string to, string subject, string html);
}