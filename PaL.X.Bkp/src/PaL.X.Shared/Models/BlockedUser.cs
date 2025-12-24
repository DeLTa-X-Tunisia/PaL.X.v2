using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaL.X.Shared.Models
{
    [Table("BloquedUsers")]
    public class BlockedUser
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int BlockedByUserId { get; set; }

        public DateTime BlockedOn { get; set; }

        public DateTime? BlockedUntil { get; set; }

        public int? DurationDays { get; set; }

        public bool IsPermanent { get; set; }

        [MaxLength(2000)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Gender { get; set; } = string.Empty;

        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Status { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(BlockedByUserId))]
        public User BlockedByUser { get; set; } = null!;
    }
}
