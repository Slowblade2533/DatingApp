using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public void Add(AppUser user)
    {
        context.Users.Add(user);
    }

    public Task<bool> EmailExistAsync(string email, CancellationToken ct = default)
    {
        return context.Users
            .AsNoTracking()
            .AnyAsync(x => x.Email == email, ct);
    }

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email, ct);
    }

    public Task<AppUser?> GetByEmailForAuthAsync(string email, CancellationToken ct = default)
    {
        return context.Users
            .Include(x => x.RefreshTokens)
            .FirstOrDefaultAsync(x => x.Email == email, ct);
    }

    public Task<AppUser?> GetByIdAsync(string userId, CancellationToken ct = default)
    {
        return context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, ct);
    }

    public Task<AppUser?> GetByRefreshTokenHashForAuthAsync(
        string refreshTokenHash, CancellationToken ct = default)
    {
        return context.Users
            .Include(x => x.RefreshTokens)
            .FirstOrDefaultAsync(
                x => x.RefreshTokens.Any(rt => rt.TokenHash == refreshTokenHash),
                ct);
    }

    public async Task<bool> SaveAllAsync(CancellationToken ct = default)
    {
        return await context.SaveChangesAsync(ct) > 0;
    }

}
