using Application.Identity.Tokens;
using Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Shared.Constants;

namespace Host.Controllers.Common;

public sealed class TokensController(ITokenService tokenService) : BaseNoAuthController
{
    [HttpPost]
    [OpenApiOperation("Request an access token using credentials.", "")]
    public async Task<IActionResult> GetTokenAsync(TokenRequest request, CancellationToken cancellationToken)
    {
        var result = await tokenService.GetTokenAsync(request, GetIpAddress()!, cancellationToken);
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("refresh")]
    [OpenApiOperation("Request an access token using a refresh token.", "")]
    [ApiConventionMethod(typeof(ApiConventions), nameof(ApiConventions.Search))]
    public async Task<IActionResult> RefreshAsync(RefreshTokenRequest request)
    {
        var result = await tokenService.RefreshTokenAsync(request, GetIpAddress()!);
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    private string? GetIpAddress() =>
        Request.Headers.ContainsKey("X-Forwarded-For")
            ? Request.Headers["X-Forwarded-For"]
            : HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "N/A";
}