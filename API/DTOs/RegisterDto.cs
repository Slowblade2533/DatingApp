using System.ComponentModel.DataAnnotations;
namespace API.DTOs;

public class RegisterDto
{
    [Required]
    [MaxLength(50)]
    public required string DisplayName { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public required string Email { get; set; }

    [Required]
    [MinLength(8)]
    [MaxLength(128)] // ← ป้องกัน HashDoS ที่สำคัญมาก
    public required string Password { get; set; }
}
