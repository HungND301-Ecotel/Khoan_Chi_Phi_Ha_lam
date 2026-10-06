using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Persistence.Catalog.User;
using Application.Dto.Authorization.Role;
using Application.Identity.Tokens;
using Application.Interfaces.Services;
using Domain.Entities.Identity;
using Domain.Exceptions;
using Infrastructure.Auth.Jwt;
using Mapster;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
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

    }

    public async Task<TokenResponse> GetTokenAsync(TokenRequest request, string ipAddress, CancellationToken cancellationToken)
    {
        var userLogin = await _userService.GetLoginResultAsync(request.Username, request.Password);
        return await GenerateTokensAndUpdateUser(userLogin, ipAddress);
    }

    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new UnauthorizedException("Invalid refresh token");
        }

        string tokenHash = HashRefreshToken(request.RefreshToken);
        var storedToken = await _tokenRefreshRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Token == tokenHash &&
                            x.ExpiredDate > DateTimeOffset.UtcNow,
            disableTracking: false);
        if (storedToken is null)
        {
            throw new UnauthorizedException("Invalid refresh token");
        }

        var user = await _userRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == storedToken.UserId,
            include: query => query.Include(x => x.UserRoles).ThenInclude(x => x.Role),
            disableTracking: true);

        if (user is null)
        {
            throw new UnauthorizedException("User not found");
        }

        var userDto = user.Adapt<UserDto>();
        userDto.Role = user.UserRoles.FirstOrDefault()?.Role?.Adapt<ShortRoleDto>();
        return await GenerateTokensAndUpdateUser(userDto, ipAddress, storedToken);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        string tokenHash = HashRefreshToken(refreshToken);
        var storedToken = await _tokenRefreshRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Token == tokenHash,
            disableTracking: false);
        if (storedToken is null)
        {
            return;
        }

        _tokenRefreshRepository.Delete(storedToken);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<TokenResponse> GenerateTokensAndUpdateUser(UserDto user, string ipAddress, RefreshToken? previousToken = null)
    {
        // Generate JWT token
        string token = GenerateJwt(user, ipAddress);

        // Generate refresh token with improved security
        string refreshToken = GenerateRefreshToken();
        var refreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationInDays);

        // Add or update refresh token in database
        await UpdateRefreshToken(user.Id, refreshToken, refreshTokenExpiryTime, previousToken);

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
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.TokenExpirationInMinutes),
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

    private static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private async Task UpdateRefreshToken(int userId, string token, DateTimeOffset expiredDate, RefreshToken? previousToken)
    {
        var expiredTokens = await _tokenRefreshRepository.GetAllAsync(
            predicate: x => x.UserId == userId && x.ExpiredDate < DateTimeOffset.UtcNow,
            disableTracking: false);

        if (expiredTokens.Count > 0)
        {
            _tokenRefreshRepository.Delete(expiredTokens.ToArray());
        }

        if (previousToken is not null)
        {
            _tokenRefreshRepository.Delete(previousToken);
        }

        await _tokenRefreshRepository.InsertAsync(RefreshToken.Create(userId, HashRefreshToken(token), expiredDate));
        await _unitOfWork.SaveChangesAsync();
    }
}
