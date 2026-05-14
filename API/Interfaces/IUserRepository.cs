using API.Entities;

namespace API.Interfaces;

public interface IUserRepository
{
    Task<bool> EmailExistAsync(string email, CancellationToken ct = default);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    void Add(AppUser user);
    Task<bool> SaveAllAsync(CancellationToken ct = default);
}
