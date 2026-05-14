using System.ComponentModel.DataAnnotations;

namespace API.Entities;

public class AppUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [MaxLength(50)]
    public required string DisplayName { get; set; }

    [MaxLength(256)]
    public required string Email { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }
    
    public required string PasswordHash { get; set; }

    // Navigation property
    public Member Member { get; set; } = null!;
}
