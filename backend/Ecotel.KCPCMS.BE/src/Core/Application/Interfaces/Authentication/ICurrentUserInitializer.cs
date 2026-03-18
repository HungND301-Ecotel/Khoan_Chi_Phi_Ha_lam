using System.Security.Claims;

namespace Application.Interfaces.Authentication;

public interface ICurrentUserInitializer
{
    void SetCurrentUser(ClaimsPrincipal user);

    void SetCurrentUserId(string userId);
}