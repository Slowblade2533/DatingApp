namespace API.Interfaces;

public interface IPasswordHasherService
{
    Task<string> HashPasswordAsync(string password, CancellationToken ct = default);
    Task<bool> VerifyPasswordAsync(string password, string storedHash, CancellationToken ct = default);
}
