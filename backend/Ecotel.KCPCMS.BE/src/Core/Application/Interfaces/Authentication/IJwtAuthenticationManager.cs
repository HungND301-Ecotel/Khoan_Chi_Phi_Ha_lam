using System.Security.Claims;
using Application.Dto.Authorization;

namespace Application.Interfaces.Authentication;

public interface IJwtAuthenticationManager
{
    Task<AuthenticateResultModel> Authenticate(string username, string password);

    Task<AuthenticateResultModel> Authenticate(int userId, Claim[] claims);

    Task<bool> ValidToken(int userId, string refreshToken);
}