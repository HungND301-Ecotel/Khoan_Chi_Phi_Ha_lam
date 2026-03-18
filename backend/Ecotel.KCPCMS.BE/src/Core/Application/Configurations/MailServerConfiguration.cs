namespace Application.Configurations;

public class MailServerConfiguration
{
    public bool Enable { get; set; }
    public string MailProvider { get; set; } = string.Empty;
    public MailConnectionInfo ConnectionInfo { get; set; } = new();
}

public class MailConnectionInfo
{
    public string Provider { get; set; } = string.Empty;
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; }
    public string SenderAddress { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsTrialMode { get; set; }
    public int IntervalCheck { get; set; }
    public int MaxRetry { get; set; }
    public int MaxMailPerLoop { get; set; }
}