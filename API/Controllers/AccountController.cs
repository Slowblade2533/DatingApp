using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController(
    AppDbContext context,
    IPasswordHasherService hasher,
    ITokenService tokenService) : BaseApiController
{
    [HttpPost("register")] // api/account/register
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        var email = registerDto.Email.ToLower();

        if (await EmailExists(email)) return BadRequest("Email already Taken");

        var user = new AppUser
        {
            DisplayName = registerDto.DisplayName,
            Email = email,
            PasswordHash = hasher.HashPassword(registerDto.Password)
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.ToDto(tokenService);
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto loginDto)
    {
        var email = loginDto.Email.ToLower();

        var user = await context.Users
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user == null || !hasher.VerifyPassword(loginDto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        return user.ToDto(tokenService);
    }

    private async Task<bool> EmailExists(string email)
    {
        return await context.Users
            .AnyAsync(x => x.Email == email);
    }
}
