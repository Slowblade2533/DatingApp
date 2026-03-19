using System.ComponentModel.DataAnnotations;
namespace API.DTOs;

public class LoginDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [MinLength(4)]
    public required string Password { get; set; }
}
