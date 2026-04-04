using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

/*
⚠️ CQ-01: AccountController Inject DbContext ตรง (MEDIUM)
❌ Controller ใช้ DbContext ตรง
public class AccountController(
    AppDbContext context, // ← Tight coupling
    IPasswordHasherService hasher,
    ITokenService tokenService) : BaseApiController
ปัญหา:
    ละเมิด Repository Pattern ที่ใช้กับ MembersController
    Controller ตรงนี้ mix concerns: มีทั้ง Business Logic และ Data Access
แนะนำ: สร้าง IUserRepository หรือ IAccountService
*/
public class AccountController(
    AppDbContext context,
    IPasswordHasherService hasher,
    ITokenService tokenService) : BaseApiController
{
    [HttpPost("register")] // api/account/register
    [EnableRateLimiting("LoginPolicy")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        var email = registerDto.Email.ToLower();
        if (await EmailExists(email)) return BadRequest("Email already Taken");

        var hashedPassword = await hasher.HashPasswordAsync(registerDto.Password);

        var user = new AppUser
        {
            DisplayName = registerDto.DisplayName,
            Email = email,
            PasswordHash = hashedPassword
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.ToDto(tokenService);
    }

    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto loginDto)
    {
        var email = loginDto.Email.ToLower();

        var user = await context.Users
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user == null || !await hasher.VerifyPasswordAsync(loginDto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        return user.ToDto(tokenService);
    }

    private async Task<bool> EmailExists(string email)
    {
        return await context.Users
            .AnyAsync(x => x.Email == email);
    }
}
