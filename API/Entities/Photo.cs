using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.Entities;

public class Photo
{
    public int Id { get; set; }

    [MaxLength(500)]
    public required string Url { get; set; }

    [MaxLength(255)]
    public string? PublicId { get; set; }

    // Navigation Property
    [JsonIgnore]
    public Member Member { get; set; } = null!;

    public string MemberId { get; set; } = null!;
}