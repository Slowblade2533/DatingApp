namespace API.Interfaces;

public interface IPasswordHasherService
{
    Task<string> HashPasswordAsync(string password);
    Task<bool> VerifyPasswordAsync(string password, string storedHash);
}
