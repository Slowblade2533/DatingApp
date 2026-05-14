using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class MemberUpdateDto
{
    [MaxLength(50)]
    public string? DisplayName { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(100)]
    public string? Country { get; set; }
}
