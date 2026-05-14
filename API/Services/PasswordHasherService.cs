using System.Security.Cryptography;
using Isopoh.Cryptography.Argon2;
using System.Text;
using API.Interfaces;

namespace API.Services;

public sealed class PasswordHasherService : IPasswordHasherService
{
    private const int SaltSize = 16; // 128-bit
    private const int HashSize = 32; // 256-bit
    private const int Parallelism = 4;
    private const int Iterations = 4;
    private const int MemorySize = 65536; // 64MB

    private static readonly SemaphoreSlim Argon2Gate = new(Math.Max(1, Math.Min(Environment.ProcessorCount / 2, 4)));

    public async Task<string> HashPasswordAsync(string password, CancellationToken ct = default)
    {
        await Argon2Gate.WaitAsync(ct);

        try
        {
            return await Task.Run(() => HashPasswordCore(password), ct);
        }
        finally
        {
            Argon2Gate.Release();
        }
    }

    public async Task<bool> VerifyPasswordAsync(string password, string storedHash, CancellationToken ct = default)
    {
        await Argon2Gate.WaitAsync(ct);

        try
        {
            return await Task.Run(() => Argon2.Verify(storedHash, password), ct);
        }
        finally
        {
            Argon2Gate.Release();
        }
    }

    private static string HashPasswordCore(string password)
    {
        var config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing,
            Version = Argon2Version.Nineteen,
            TimeCost = Iterations,
            MemoryCost = MemorySize,
            Lanes = Parallelism,
            Threads = 1,
            Password = Encoding.UTF8.GetBytes(password),
            Salt = RandomNumberGenerator.GetBytes(SaltSize),
            HashLength = HashSize
        };

        using var argon2 = new Argon2(config);
        using var hash = argon2.Hash();

        return config.EncodeString(hash.Buffer);
    }
}
