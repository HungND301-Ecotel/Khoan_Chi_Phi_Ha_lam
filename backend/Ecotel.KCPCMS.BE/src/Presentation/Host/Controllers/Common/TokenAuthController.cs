using Application.Identity.Tokens;
using Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Shared.Constants;

namespace Host.Controllers.Common;

public sealed class TokensController(ITokenService tokenService) : BaseNoAuthController
{
    private const string RefreshCookieName = "refresh_token";
    private const string RefreshCookiePath = "/api/v1/tokens";

    [HttpPost]
    [OpenApiOperation("Request an access token using credentials.", "")]
    public async Task<IActionResult> GetTokenAsync(TokenRequest request, CancellationToken cancellationToken)
    {
        var result = await tokenService.GetTokenAsync(request, GetIpAddress()!, cancellationToken);
        SetRefreshCookie(result);
        return Ok(new { result.Token }, MessageCommon.GetDataSuccess);
    }

    [HttpPost("refresh")]
    [OpenApiOperation("Request an access token using a refresh token.", "")]
    [ApiConventionMethod(typeof(ApiConventions), nameof(ApiConventions.Search))]
    public async Task<IActionResult> RefreshAsync()
    {
        var refreshToken = Request.Cookies[RefreshCookieName] ?? string.Empty;
        var result = await tokenService.RefreshTokenAsync(new RefreshTokenRequest(refreshToken), GetIpAddress()!);
        SetRefreshCookie(result);
        return Ok(new { result.Token }, MessageCommon.GetDataSuccess);
    }

    [HttpPost("revoke")]
    [OpenApiOperation("Revoke the current refresh token and sign out.", "")]
    public async Task<IActionResult> RevokeAsync()
    {
        await tokenService.RevokeTokenAsync(Request.Cookies[RefreshCookieName] ?? string.Empty);
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath
        });
        return Ok(new { }, MessageCommon.GetDataSuccess);
    }

    private void SetRefreshCookie(TokenResponse result) =>
        Response.Cookies.Append(RefreshCookieName, result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = result.RefreshTokenExpiryTime,
            IsEssential = true
        });

    private string? GetIpAddress() =>
        Request.Headers.ContainsKey("X-Forwarded-For")
            ? Request.Headers["X-Forwarded-For"]
            : HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "N/A";
}
