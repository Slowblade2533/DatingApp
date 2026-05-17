using API.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API.Interfaces;

public interface IAccountService
{
    Task<ActionResult<AuthResultDto>> RegisterAsync(RegisterDto registerDto, CancellationToken ct = default);
    Task<ActionResult<AuthResultDto>> LoginAsync(LoginDto loginDto, CancellationToken ct = default);
    Task<ActionResult<AuthResultDto>> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task<ActionResult<UserDto>> GetCurrentUserAsync(string userId, CancellationToken ct = default);
    Task<ActionResult> LogoutAsync(string refreshToken, CancellationToken ct = default);
}
