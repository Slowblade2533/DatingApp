using System.ComponentModel.DataAnnotations;
namespace API.DTOs;

public class LoginDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public required string Email { get; set; }

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public required string Password { get; set; }
}
