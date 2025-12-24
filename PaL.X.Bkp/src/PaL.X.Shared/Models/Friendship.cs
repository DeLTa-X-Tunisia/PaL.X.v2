using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaL.X.Shared.Models
{
    public class Friendship
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public int FriendId { get; set; }
        
        [ForeignKey("FriendId")]
        public User Friend { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsBlocked { get; set; } = false;
    }
}
