using API.Entities;
using API.DTOs;

namespace API.Interfaces;

public interface ITokenService
{
    TokenResultDto CreateToken(AppUser user);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
