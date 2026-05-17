namespace API.DTOs;

public sealed record AuthResultDto(
    UserDto User,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
