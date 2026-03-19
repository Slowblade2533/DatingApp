using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using System.Text;
using API.Interfaces;

namespace API.Services;

public class PasswordHasherService : IPasswordHasherService
{
    private const int SaltSize = 16; // 128-bit
    private const int HashSize = 32; // 256-bit
    private const int Parallelism = 4;
    private const int Iterations = 4;
    private const int MemorySize = 65536; // 64MB

    public string HashPassword(string password)
    {
        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        var hash = Compute(password, salt);

        var combined = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, combined, 0, SaltSize);
        Array.Copy(hash, 0, combined, SaltSize, HashSize);

        return Convert.ToBase64String(combined);
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        var combined = Convert.FromBase64String(storedHash);

        var salt = new byte[SaltSize];
        var hash = new byte[HashSize];
        Array.Copy(combined, 0, salt, 0, SaltSize);
        Array.Copy(combined, SaltSize, hash, 0, HashSize);

        var newHash = Compute(password, salt);

        return CryptographicOperations.FixedTimeEquals(hash, newHash);
    }

    private static byte[] Compute(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = Parallelism,
            Iterations = Iterations,
            MemorySize = MemorySize
        };
        return argon2.GetBytes(HashSize);
    }
}
