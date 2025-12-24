using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaL.X.Shared.Models
{
    public class FriendRequest
    {
        [Key]
        public int Id { get; set; }

        public int SenderId { get; set; }
        
        [ForeignKey("SenderId")]
        public User Sender { get; set; } = null!;

        public int ReceiverId { get; set; }
        
        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; } = null!;

        public string Status { get; set; } = "Pending"; // Pending, Accepted, AcceptedMutual, Refused
        
        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
