using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaL.X.Shared.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        public int SenderId { get; set; }
        [ForeignKey("SenderId")]
        public User Sender { get; set; } = null!;

        public int ReceiverId { get; set; }
        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; } = null!;

        public string Content { get; set; } = string.Empty;
        
        public string ContentType { get; set; } = "Text"; // Text, RTF, Image, etc.

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public bool IsEdited { get; set; } = false;
    }
}
