using System.Security.Claims;

namespace Application.Interfaces.Authentication;

public interface ICurrentUser
{
    string? Name { get; }

    int GetUserId();

    string? GetUserEmail();

    string? GetTenant();

    bool IsAuthenticated();

    bool IsInRole(string role);

    IEnumerable<Claim>? GetUserClaims();
}