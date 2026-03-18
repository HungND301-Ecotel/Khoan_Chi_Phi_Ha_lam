namespace Application.Dto.Integrates.Emails;

public class SendEmailNotificationModel
{
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long? LostReportId { get; set; }
    public long? FoundReportId { get; set; }

    public List<string> Recipients { get; set; } = new();
}