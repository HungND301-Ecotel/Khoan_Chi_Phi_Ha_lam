namespace Application.Configurations;

public class QuartzConfiguration
{
    public bool Enabled { get; set; }
    public QuartzSetting AutoCancelAppointment { get; set; } = default!;
}

public class QuartzSetting
{
    public bool Enabled { get; set; }
    public string Scheduled { get; set; } = string.Empty;
}