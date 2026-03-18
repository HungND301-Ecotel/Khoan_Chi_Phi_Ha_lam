using System.Security.Claims;
using Application.Common.Interfaces;
using Domain.Exceptions;
using Shared.Constants;

namespace Infrastructure.Auth;

public class CurrentUser : ICurrentUser, ICurrentUserInitializer
{
    private ClaimsPrincipal? _user;

    public string? Name => _user?.Identity?.Name;

    private int _userId;

    public int GetUserId() =>
        IsAuthenticated()
            ? UserId
            : _userId;

    public string GetUserEmail() =>
        IsAuthenticated()
            ? Email
            : string.Empty;

    public bool IsAuthenticated() =>
        _user?.Identity?.IsAuthenticated is true;

    public bool IsInRole(string role) =>
        _user?.IsInRole(role) is true;

    public IEnumerable<Claim>? GetUserClaims() =>
        _user?.Claims;

    public void SetCurrentUser(ClaimsPrincipal user)
    {
        if (_user != null)
        {
            throw new Exception("Method reserved for in-scope initialization");
        }

        _user = user;
    }

    public void SetCurrentUserId(int userId)
    {
        if (_userId != 0)
        {
            throw new Exception("Method reserved for in-scope initialization");
        }

        if (userId != 0)
        {
            _userId = userId;
        }
    }

    public int UserId => _user?.FindFirstValue(SystemClaims.NameIdentifier) is { } userId
        ? int.Parse(userId)
        : throw new UnauthorizedException("NO_CURRENT_USER");

    public string Email => _user?.FindFirstValue(SystemClaims.Email) ?? string.Empty;

    public string Avatar => _user?.FindFirstValue(SystemClaims.Avatar) ?? string.Empty;
}