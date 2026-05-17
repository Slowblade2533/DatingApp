using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<AppUser> Users { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
        builder.Entity<RefreshToken>().HasIndex(rt => rt.TokenHash).IsUnique();
        builder.Entity<RefreshToken>().HasIndex(rt => new { rt.UserId, rt.ExpiresAt });
        builder.Entity<Photo>().HasIndex(p => p.MemberId);
        builder.Entity<Member>().HasIndex(m => m.Gender);
        builder.Entity<Member>().HasIndex(m => new { m.City, m.Country });
    }
}
