using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class TokenBlacklist
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(512)]
        public string TokenHash { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        public DateTime RevokedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        [MaxLength(50)]
        public string Reason { get; set; } = "Manual revocation";

        // Navigation
        public User? User { get; set; }
    }
}