using API.DTOs;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;

namespace API.Controllers;

public class AccountController(
    IAccountService accountService,
    IOptions<AuthCookieSettings> cookieOptions,
    IWebHostEnvironment env) : BaseApiController
{
    private readonly AuthCookieSettings _cookieSettings = cookieOptions.Value;

    [AllowAnonymous]
    [HttpPost("register")] // api/account/register
    [EnableRateLimiting("LoginPolicy")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto, CancellationToken ct = default)
    {
        var result = await accountService.RegisterAsync(registerDto, ct);
        return ApplyCookiesAndReturnUser(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto loginDto, CancellationToken ct = default)
    {
        var result = await accountService.LoginAsync(loginDto, ct);
        return ApplyCookiesAndReturnUser(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [EnableRateLimiting("RefreshPolicy")]
    public async Task<ActionResult<UserDto>> Refresh(CancellationToken ct = default)
    {
        var refreshToken = Request.Cookies[_cookieSettings.RefreshTokenName];
        var result = await accountService.RefreshAsync(refreshToken ?? string.Empty, ct);
        return ApplyCookiesAndReturnUser(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken ct = default)
    {
        var userId = User.GetMemberId();
        var result = await accountService.GetCurrentUserAsync(userId, ct);

        if (result.Result is not null)
            return result.Result;

        var user = result.Value;
        if (user is null)
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);

        if (TryGetJwtExpirationUtc(user.Token, out var expiresAt))
        {
            SetAccessCookie(user.Token, expiresAt);
        }

        return Ok(user);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [EnableRateLimiting("RefreshPolicy")]
    public async Task<ActionResult> Logout(CancellationToken ct = default)
    {
        var refreshToken = Request.Cookies[_cookieSettings.RefreshTokenName];
        await accountService.LogoutAsync(refreshToken ?? string.Empty, ct);
        ClearAuthCookies();
        return NoContent();
    }

    private ActionResult<UserDto> ApplyCookiesAndReturnUser(ActionResult<AuthResultDto> result)
    {
        if (result.Result is not null)
            return result.Result;

        var authResult = result.Value;
        if (authResult is null)
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);

        SetAuthCookies(authResult);
        return Ok(authResult.User);
    }

    private void SetAuthCookies(AuthResultDto authResult)
    {
        SetAccessCookie(authResult.User.Token, authResult.AccessTokenExpiresAt);

        var secure = !env.IsDevelopment() || Request.IsHttps;
        Response.Cookies.Append(
            _cookieSettings.RefreshTokenName,
            authResult.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Expires = authResult.RefreshTokenExpiresAt,
                Path = "/"
            });
    }

    private void SetAccessCookie(string token, DateTime expiresAt)
    {
        var secure = !env.IsDevelopment() || Request.IsHttps;

        Response.Cookies.Append(
            _cookieSettings.AccessTokenName,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Expires = expiresAt,
                Path = "/"
            });
    }

    private void ClearAuthCookies()
    {
        var secure = !env.IsDevelopment() || Request.IsHttps;
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.None,
            IsEssential = true,
            Expires = DateTimeOffset.UnixEpoch,
            Path = "/"
        };

        Response.Cookies.Delete(_cookieSettings.AccessTokenName, options);
        Response.Cookies.Delete(_cookieSettings.RefreshTokenName, options);
    }

    private static bool TryGetJwtExpirationUtc(string token, out DateTime expiresAt)
    {
        expiresAt = default;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            expiresAt = jwtToken.ValidTo;
            return expiresAt > DateTime.UnixEpoch;
        }
        catch
        {
            return false;
        }
    }
}
