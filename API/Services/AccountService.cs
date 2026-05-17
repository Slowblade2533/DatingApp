using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace API.Services;

public class AccountService(
    IUserRepository userRepository,
    IPasswordHasherService hasher,
    ITokenService tokenService,
    IOptions<AuthCookieSettings> authCookieOptions) : IAccountService
{
    private readonly TimeSpan _refreshTokenLifetime = TimeSpan.FromDays(
        Math.Clamp(authCookieOptions.Value.RefreshTokenExpireDays, 1, 30));

    public async Task<ActionResult<AuthResultDto>> LoginAsync(LoginDto loginDto, CancellationToken ct = default)
    {
        var email = loginDto.Email.Trim().ToLowerInvariant();

        var user = await userRepository.GetByEmailForAuthAsync(email, ct);

        if (user is null || !await hasher.VerifyPasswordAsync(loginDto.Password, user.PasswordHash, ct))
            return new UnauthorizedObjectResult("Invalid credentials");

        var authResult = CreateAuthResultAndRotateRefreshToken(user);

        if (!await userRepository.SaveAllAsync(ct))
            return new BadRequestObjectResult("Unable to complete login.");

        return authResult;
    }

    public async Task<ActionResult<AuthResultDto>> RegisterAsync(RegisterDto registerDto, CancellationToken ct = default)
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

        var authResult = CreateAuthResultAndRotateRefreshToken(user);

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

        return authResult;
    }

    public async Task<ActionResult<AuthResultDto>> RefreshAsync(
        string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return new UnauthorizedObjectResult("Missing refresh token.");

        var refreshTokenHash = tokenService.HashRefreshToken(refreshToken);
        var user = await userRepository.GetByRefreshTokenHashForAuthAsync(refreshTokenHash, ct);

        if (user is null)
            return new UnauthorizedObjectResult("Invalid refresh token.");

        var storedRefreshToken = user.RefreshTokens.SingleOrDefault(rt => rt.TokenHash == refreshTokenHash);
        if (storedRefreshToken is null)
            return new UnauthorizedObjectResult("Invalid refresh token.");

        if (storedRefreshToken.RevokedAt is not null)
        {
            // Reuse detection: if an already-revoked token is replayed, revoke active sessions.
            RevokeActiveRefreshTokens(user);
            await userRepository.SaveAllAsync(ct);
            return new UnauthorizedObjectResult("Refresh token is no longer valid.");
        }

        if (storedRefreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            storedRefreshToken.RevokedAt = DateTime.UtcNow;
            await userRepository.SaveAllAsync(ct);
            return new UnauthorizedObjectResult("Refresh token expired.");
        }

        var authResult = CreateAuthResultAndRotateRefreshToken(user, storedRefreshToken);

        if (!await userRepository.SaveAllAsync(ct))
            return new BadRequestObjectResult("Unable to refresh session.");

        return authResult;
    }

    public async Task<ActionResult<UserDto>> GetCurrentUserAsync(
        string userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return new UnauthorizedObjectResult("User not found.");

        var tokenResult = tokenService.CreateToken(user);
        return user.ToDto(tokenResult.Token);
    }

    public async Task<ActionResult> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return new NoContentResult();

        var refreshTokenHash = tokenService.HashRefreshToken(refreshToken);
        var user = await userRepository.GetByRefreshTokenHashForAuthAsync(refreshTokenHash, ct);

        if (user is null)
            return new NoContentResult();

        var storedRefreshToken = user.RefreshTokens.SingleOrDefault(rt => rt.TokenHash == refreshTokenHash);
        if (storedRefreshToken is null || storedRefreshToken.RevokedAt is not null)
            return new NoContentResult();

        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        await userRepository.SaveAllAsync(ct);

        return new NoContentResult();
    }

    private AuthResultDto CreateAuthResultAndRotateRefreshToken(
        AppUser user, RefreshToken? currentRefreshToken = null)
    {
        var newRefreshToken = tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = tokenService.HashRefreshToken(newRefreshToken);
        var refreshExpiresAt = DateTime.UtcNow.Add(_refreshTokenLifetime);

        if (currentRefreshToken is not null)
        {
            currentRefreshToken.RevokedAt = DateTime.UtcNow;
            currentRefreshToken.ReplacedByTokenHash = newRefreshTokenHash;
        }

        user.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = newRefreshTokenHash,
            ExpiresAt = refreshExpiresAt,
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        });

        PruneRefreshTokens(user);

        var tokenResult = tokenService.CreateToken(user);
        var userDto = user.ToDto(tokenResult.Token);

        return new AuthResultDto(
            userDto,
            tokenResult.ExpiresAt,
            newRefreshToken,
            refreshExpiresAt);
    }

    private static void RevokeActiveRefreshTokens(AppUser user)
    {
        var now = DateTime.UtcNow;
        foreach (var token in user.RefreshTokens.Where(rt => rt.RevokedAt is null && rt.ExpiresAt > now))
        {
            token.RevokedAt = now;
        }
    }

    private static void PruneRefreshTokens(AppUser user)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-14);

        user.RefreshTokens.RemoveAll(rt =>
            rt.ExpiresAt <= now &&
            rt.RevokedAt is not null &&
            rt.RevokedAt <= cutoff);

        const int maxTokensPerUser = 20;
        if (user.RefreshTokens.Count <= maxTokensPerUser)
            return;

        foreach (var staleToken in user.RefreshTokens
                     .OrderByDescending(rt => rt.CreatedAt)
                     .Skip(maxTokensPerUser)
                     .ToList())
        {
            user.RefreshTokens.Remove(staleToken);
        }
    }
}
