namespace API.DTOs;

public sealed record TokenResultDto(
    string Token,
    DateTime ExpiresAt);
