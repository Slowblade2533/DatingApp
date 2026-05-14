using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class AccountService(
    IUserRepository userRepository,
    IPasswordHasherService hasher,
    ITokenService tokenService) : IAccountService
{
    public async Task<ActionResult<UserDto>> LoginAsync(LoginDto loginDto, CancellationToken ct = default)
    {
        var email = loginDto.Email.Trim().ToLowerInvariant();

        var user = await userRepository.GetByEmailAsync(email, ct);

        if (user is null || !await hasher.VerifyPasswordAsync(loginDto.Password, user.PasswordHash, ct))
            return new UnauthorizedObjectResult("Invalid credentials");

        return CreateUserDto(user);
    }

    public async Task<ActionResult<UserDto>> RegisterAsync(RegisterDto registerDto, CancellationToken ct = default)
    {
        var email = registerDto.Email.Trim().ToLowerInvariant();

        if (await userRepository.EmailExistAsync(email, ct))
            return new BadRequestObjectResult("Email already taken");

        var user = new AppUser
        {
            DisplayName = registerDto.DisplayName.Trim(),
            Email = email,
            PasswordHash = await hasher.HashPasswordAsync(registerDto.Password, ct)
        };

        userRepository.Add(user);

        try
        {
            await userRepository.SaveAllAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await userRepository.EmailExistAsync(email, CancellationToken.None))
                return new BadRequestObjectResult("Email already taken");

            throw;
        }

        return CreateUserDto(user);
    }

    private UserDto CreateUserDto(AppUser user)
    {
        var token = tokenService.CreateToken(user);

        return user.ToDto(token);
    }
}
