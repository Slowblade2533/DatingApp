namespace API.DTOs;

public class MemberDto
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public string? ImageUrl { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
    public required string Gender { get; set; }
    public DateOnly DateofBirth { get; set; }
}
