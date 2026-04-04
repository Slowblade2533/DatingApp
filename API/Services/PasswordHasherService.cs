using System.Security.Cryptography;
using Isopoh.Cryptography.Argon2;
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

    public async Task<string> HashPasswordAsync(string password)
    {
        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        // 1. นำ Parameter ทั้งหมดมากำหนดใน Argon2Config
        var config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing,
            Version = Argon2Version.Nineteen,
            TimeCost = Iterations,
            MemoryCost = MemorySize,
            Lanes = Parallelism,
            Threads = 1, // ใช้ 1 Thread ต่อการ Hash 1 ครั้ง เพื่อป้องกัน Thread Pool ของ API ทำงานหนักเกินไป
            Password = Encoding.UTF8.GetBytes(password),
            Salt = salt,
            HashLength = HashSize
        };

        // 2. นำไปรันใน Task.Run เพื่อไม่ให้ Block Thread หลักของ Web API
        return await Task.Run(() =>
        {
            using var argon2 = new Argon2(config);
            using var hash = argon2.Hash();

            // EncodeString จะรวมทุกอย่างและคืนค่าออกมาเป็น PHC Format อัตโนมัติ
            // ตัวอย่าง: $argon2id$v=19$m=65536,t=4,p=4$SaltBase64$HashBase64
            return config.EncodeString(hash.Buffer);
        });
    }

    public async Task<bool> VerifyPasswordAsync(string password, string storedHash)
    {
        return await Task.Run(() => Argon2.Verify(storedHash, password));
    }
}
