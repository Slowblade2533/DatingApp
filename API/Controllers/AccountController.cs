using API.DTOs;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers;

public class AccountController(IAccountService accountService) : BaseApiController
{
    [HttpPost("register")] // api/account/register
    [EnableRateLimiting("LoginPolicy")]
    public Task<ActionResult<UserDto>> Register(
        RegisterDto registerDto,
        CancellationToken ct)
    {
        return accountService.RegisterAsync(registerDto, ct);
    }

    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    public Task<ActionResult<UserDto>> Login([FromBody] LoginDto loginDto, CancellationToken ct)
    {
        return accountService.LoginAsync(loginDto, ct);
    }
}
