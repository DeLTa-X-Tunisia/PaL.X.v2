using System;

namespace PaL.X.Shared.DTOs
{
    public class VideoCallEventDto
    {
        public string EventType { get; set; } = string.Empty; // started | ended
        public DateTime AtUtc { get; set; } = DateTime.UtcNow;
    }
}
