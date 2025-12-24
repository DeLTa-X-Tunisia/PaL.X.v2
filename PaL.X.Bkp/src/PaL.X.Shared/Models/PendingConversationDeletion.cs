using System;
using System.ComponentModel.DataAnnotations;

namespace PaL.X.Shared.Models
{
    public class PendingConversationDeletion
    {
        [Key]
        public int Id { get; set; }

        public int RequesterId { get; set; }
        public int RecipientId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsNotified { get; set; } = false;
    }
}
