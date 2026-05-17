namespace API.Helpers;

public class AuthCookieSettings
{
    public string AccessTokenName { get; set; } = "da_access";
    public string RefreshTokenName { get; set; } = "da_refresh";
    public int RefreshTokenExpireDays { get; set; } = 7;
}
