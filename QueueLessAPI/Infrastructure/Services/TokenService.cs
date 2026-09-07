using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Configurations.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    public TokenResponse GenerateTokens(
        string userId,
        string email,
        IEnumerable<string> roles)
    {
        var now = DateTime.UtcNow;

        var accessTokenExpiresAt = now.AddMinutes(
            _jwtSettings.AccessTokenExpiryMinutes);

        var accessToken = GenerateAccessToken(
            userId,
            email,
            roles,
            now,
            accessTokenExpiresAt);

        var refreshToken = GenerateRefreshToken();

        return new TokenResponse(
            accessToken,
            refreshToken,
            accessTokenExpiresAt);
    }

    private string GenerateAccessToken(
        string userId,
        string email,
        IEnumerable<string> roles,
        DateTime issuedAt,
        DateTime expiresAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),

            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email)
        };

        foreach (var role in roles)
        {
            claims.Add(
                new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.Key));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(randomBytes);
    }
}