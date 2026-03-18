namespace Application.Configurations;

public class AuthenticationConfiguration
{
    public AuthenticationTokenConfiguration Token { get; set; } = new();
    public AuthenticationGoogleConfiguration Google { get; set; } = new();
    public AuthenticationFacebookConfiguration Facebook { get; set; } = new();
}

public class AuthenticationTokenConfiguration
{
    public string Expired { get; set; } = "1440";
    public string RefreshExpired { get; set; } = "5";
}

public class AuthenticationGoogleConfiguration
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class AuthenticationFacebookConfiguration
{
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
}