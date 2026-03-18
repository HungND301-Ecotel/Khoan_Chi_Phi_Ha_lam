namespace Application.Dto.Authorization;

public class AuthenticateResultModel
{
    public string JwtToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}