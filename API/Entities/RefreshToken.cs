using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace API.Entities;

public class RefreshToken
{
    public int Id { get; set; }

    [MaxLength(64)]
    public required string TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    [MaxLength(64)]
    public string? ReplacedByTokenHash { get; set; }

    public string UserId { get; set; } = null!;

    [JsonIgnore]
    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; } = null!;
}
