using Application.Dto.Authorization;

namespace Application.Interfaces.Authentication;

public interface ITokenRefresher
{
    Task<AuthenticateResultModel> Refresh(RefreshTokenModel refreshInput);
}