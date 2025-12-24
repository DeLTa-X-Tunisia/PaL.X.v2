using System;

namespace PaL.X.Shared.DTOs
{
    public class ChatMessageDto
    {
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ContentType { get; set; } = "Text"; // Text, Image, File, Link
        public DateTime Timestamp { get; set; }
        public bool IsEdited { get; set; } = false;
        public List<string> SmileyFilenames { get; set; } = new List<string>();

        /// <summary>
        /// Client-generated correlation id for deduplicating pending bubbles on sender side.
        /// Echoed back by the server (not persisted) so the sender can match the ack to the in-flight bubble.
        /// </summary>
        public string? ClientTempId { get; set; }

        // Attachment metadata (optional)
        public string? FileName { get; set; }
        public long? FileSizeBytes { get; set; }
        public double? DurationSeconds { get; set; }
    }
}
