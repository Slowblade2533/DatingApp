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

    public async Task<bool> SaveAllAsync(CancellationToken ct = default)
    {
        return await context.SaveChangesAsync(ct) > 0;
    }

}
