using API.Entities;
using System.ComponentModel.DataAnnotations.Schema;
namespace API.DTOs;

public class SeedUserDto
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public DateOnly DateofBirth { get; set; }
    public string? ImageUrl { get; set; }
    public required string DisplayName { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime LastActive { get; set; } = DateTime.UtcNow;
    public required string Gender { get; set; }
    public string? Description { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }

    // Navigation property
    [ForeignKey(nameof(Id))]
    public AppUser User { get; set; } = null!;
}
