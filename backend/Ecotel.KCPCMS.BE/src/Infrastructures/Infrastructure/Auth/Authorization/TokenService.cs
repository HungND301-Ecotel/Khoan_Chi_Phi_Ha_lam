using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Persistence.Catalog.User;
using Application.Identity.Tokens;
using Application.Interfaces.Services;
using Domain.Entities.Identity;
using Domain.Exceptions;
using Infrastructure.Auth.Jwt;
using Mapster;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Constants;

namespace Infrastructure.Auth.Authorization;

internal class TokenService : ITokenService
{
    private readonly IWriteRepository<User> _userRepository;
    private readonly IWriteRepository<RefreshToken> _tokenRefreshRepository;
    private readonly IUserService _userService;
    private readonly JwtSettings _jwtOptions;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly IUnitOfWork _unitOfWork;

    public TokenService(
        IUnitOfWork unitOfWork,
        IUserService userService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userRepository = unitOfWork.GetRepository<User>();
        _tokenRefreshRepository = unitOfWork.GetRepository<RefreshToken>();
        _userService = userService;
        _jwtOptions = jwtSettings.Value;
        _unitOfWork = unitOfWork;

        // Initialize reusable objects once in constructor
        byte[] secret = Encoding.UTF8.GetBytes(_jwtOptions.Key);
        var securityKey = new SymmetricSecurityKey(secret);
        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = securityKey,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = JwtAuthConstants.Audience,
            ValidIssuer = JwtAuthConstants.Issuer,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };
    }

    public async Task<TokenResponse> GetTokenAsync(TokenRequest request, string ipAddress, CancellationToken cancellationToken)
    {
        var userLogin = await _userService.GetLoginResultAsync(request.Username, request.Password);
        return await GenerateTokensAndUpdateUser(userLogin, ipAddress);
    }

    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress)
    {
        // Validate input parameters
        if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.RefreshToken))
        {
            throw new UnauthorizedException("Invalid token or refresh token");
        }

        var userPrincipal = GetPrincipalFromExpiredToken(request.Token);
        string? nameIdentifier = userPrincipal.FindFirstValue(SystemClaims.NameIdentifier);

        if (!int.TryParse(nameIdentifier, out int userId))
        {
            throw new SecurityTokenException("Invalid token: unable to determine user ID");
        }

        // Verify refresh token is valid
        if (!await IsValidRefreshToken(userId, request.RefreshToken))
        {
            throw new SecurityTokenException("Invalid refresh token");
        }

        // Get user with single efficient query
        var user = await _userRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == userId,
            disableTracking: true);

        if (user is null)
        {
            throw new UnauthorizedException("User not found");
        }

        return await GenerateTokensAndUpdateUser(user.Adapt<UserDto>(), ipAddress);
    }

    private async Task<TokenResponse> GenerateTokensAndUpdateUser(UserDto user, string ipAddress)
    {
        // Generate JWT token
        string token = GenerateJwt(user, ipAddress);

        // Generate refresh token with improved security
        string refreshToken = GenerateRefreshToken();
        var refreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationInDays);

        // Add or update refresh token in database
        await UpdateRefreshToken(user.Id, refreshToken, refreshTokenExpiryTime);

        return new TokenResponse(token, refreshToken, refreshTokenExpiryTime);
    }

    private string GenerateJwt(UserDto user, string ipAddress)
    {
        var claims = new List<Claim>
        {
            new(SystemClaims.NameIdentifier, user.Id.ToString()),
            new(SystemClaims.Email, user.Email),
            new(SystemClaims.Fullname, $"{user.Fullname}".Trim()),
            new(SystemClaims.IpAddress, ipAddress),
            new(SystemClaims.MobilePhone, user.PhoneNumber)
        };

        if (user.Role != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Role.RoleType.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: JwtAuthConstants.Issuer,
            audience: JwtAuthConstants.Audience,
            claims: claims,
            //expires: DateTime.UtcNow.AddMinutes(_jwtOptions.TokenExpirationInMinutes),
            expires: DateTime.UtcNow.AddYears(1),
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        byte[] randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        try
        {
            // Copy validation parameters to disable lifetime validation temporarily
            var tokenValidationParametersWithoutLifetime = _tokenValidationParameters.Clone();
            tokenValidationParametersWithoutLifetime.ValidateLifetime = false;

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(
                token,
                tokenValidationParametersWithoutLifetime,
                out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
        catch (Exception ex)
        {
            throw new UnauthorizedException($"Token validation failed: {ex.Message}");
        }
    }

    private async Task UpdateRefreshToken(int userId, string token, DateTimeOffset expiredDate)
    {
        var utcNow = DateTime.UtcNow;

        var expiredTokens = await _tokenRefreshRepository.GetAllAsync(
            predicate: x => x.UserId == userId && x.ExpiredDate < utcNow,
            disableTracking: false);

        if (expiredTokens.Count > 0)
        {
            _tokenRefreshRepository.Delete(expiredTokens.ToArray());
            await _tokenRefreshRepository.InsertAsync(RefreshToken.Create(userId, token, expiredDate));
            await _unitOfWork.SaveChangesAsync();
        }
        else
        {
            await _tokenRefreshRepository.InsertAsync(RefreshToken.Create(userId, token, expiredDate));
            await _unitOfWork.SaveChangesAsync();
        }
    }

    private async Task<bool> IsValidRefreshToken(int userId, string refreshToken)
    {
        var token = await _tokenRefreshRepository.GetFirstOrDefaultAsync(
            predicate: x => x.UserId == userId &&
                           x.Token == refreshToken &&
                           x.ExpiredDate > DateTime.UtcNow,
            disableTracking: true);

        return token != null;
    }
}