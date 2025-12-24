using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaL.X.Shared.Models
{
    /// <summary>
    /// Persists metadata for any file-based message (documents, images, audio, video, voice).
    /// Actual binaries stay on disk / upload storage; only metadata is tracked here.
    /// </summary>
    public class FileTransfer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MessageId { get; set; }
        [ForeignKey(nameof(MessageId))]
        public Message Message { get; set; } = null!;

        [Required]
        public int SenderId { get; set; }
        [ForeignKey(nameof(SenderId))]
        public User Sender { get; set; } = null!;

        [Required]
        public int ReceiverId { get; set; }
        [ForeignKey(nameof(ReceiverId))]
        public User Receiver { get; set; } = null!;

        [Required]
        [MaxLength(32)]
        public string ContentType { get; set; } = string.Empty; // image, video, audio, voice, file

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty; // original name or sanitized name

        [Required]
        [MaxLength(1024)]
        public string FileUrl { get; set; } = string.Empty; // accessible URL/path from upload API

        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Duration in seconds for audio/video payloads; null for other file kinds.
        /// </summary>
        public double? DurationSeconds { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
