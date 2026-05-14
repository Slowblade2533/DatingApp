using API.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API.Interfaces;

public interface IAccountService
{
    Task<ActionResult<UserDto>> RegisterAsync(RegisterDto registerDto, CancellationToken ct = default);
    Task<ActionResult<UserDto>> LoginAsync(LoginDto loginDto, CancellationToken ct = default);
}
