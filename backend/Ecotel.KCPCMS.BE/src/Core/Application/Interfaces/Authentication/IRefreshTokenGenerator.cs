namespace Application.Interfaces.Authentication;

public interface IRefreshTokenGenerator
{
    string GenerateToken();
}