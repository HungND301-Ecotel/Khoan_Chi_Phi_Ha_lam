using System.Security.Claims;

namespace Application.Common.Interfaces;

public interface ICurrentUser
{
    string? Name { get; }

    int GetUserId();

    string? GetUserEmail();

    bool IsAuthenticated();

    bool IsInRole(string role);

    IEnumerable<Claim>? GetUserClaims();

    int UserId { get; }
    string Email { get; }
    string Avatar { get; }
}