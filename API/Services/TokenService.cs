using API.Entities;
using API.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace API.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly SigningCredentials _credentials;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _lifetime;

    public TokenService(IConfiguration config)
    {
        var tokenKey = config["Jwt:TokenKey"] ?? config["TokenKey"]
            ?? throw new InvalidOperationException("JWT token key is not configured.");

        var tokenKeyBytes = Encoding.UTF8.GetBytes(tokenKey);

        if (tokenKeyBytes.Length < 64)
            throw new InvalidOperationException("JWT token key must be at least 64 bytes for HS512.");

        _issuer = config["Jwt:Issuer"] ?? "DatingApp-API";
        _audience = config["Jwt:Audience"] ?? "DatingApp-Client";
        _lifetime = TimeSpan.FromMinutes(config.GetValue("Jwt:ExpireMinutes", 60));

        var key = new SymmetricSecurityKey(tokenKeyBytes);
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
    }
    public string CreateToken(AppUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(_lifetime),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = _credentials
        };

        var token = _tokenHandler.CreateToken(descriptor);

        return _tokenHandler.WriteToken(token);
    }
}
