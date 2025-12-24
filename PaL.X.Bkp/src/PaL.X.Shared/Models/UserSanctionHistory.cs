using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaL.X.Shared.Models
{
    [Table("UserSanctionHistory")]
    public class UserSanctionHistory
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int BlockedByUserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(BlockedByUserId))]
        public User BlockedByUser { get; set; } = null!;
    }
}
