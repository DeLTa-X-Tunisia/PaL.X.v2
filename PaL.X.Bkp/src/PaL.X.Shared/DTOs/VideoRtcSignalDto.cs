using System;

namespace PaL.X.Shared.DTOs
{
    public class VideoRtcSignalDto
    {
        public string CallId { get; set; } = string.Empty;
        public int FromUserId { get; set; }
        public string SignalType { get; set; } = string.Empty; // offer | answer | ice | hangup
        public string Payload { get; set; } = string.Empty; // SDP or ICE candidate JSON
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
