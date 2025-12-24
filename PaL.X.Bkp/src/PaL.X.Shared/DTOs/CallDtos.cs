using System;

namespace PaL.X.Shared.DTOs
{
    public class CallInviteDto
    {
        public string CallId { get; set; } = Guid.NewGuid().ToString();
        public int FromUserId { get; set; }
        public string FromName { get; set; } = string.Empty;
        public int ToUserId { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }

    public class CallAcceptDto
    {
        public string CallId { get; set; } = string.Empty;
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
    }

    public class CallRejectDto
    {
        public string CallId { get; set; } = string.Empty;
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public string Reason { get; set; } = "refused";
        public DateTime RejectedAt { get; set; } = DateTime.UtcNow;
    }

    public class CallEndDto
    {
        public string CallId { get; set; } = string.Empty;
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public string Reason { get; set; } = "hangup";
        public DateTime EndedAt { get; set; } = DateTime.UtcNow;
    }

    public class CallLogDto
    {
        public string CallId { get; set; } = string.Empty;
        public int CallerId { get; set; }
        public int CalleeId { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedAt { get; set; }
        public DateTime EndedAt { get; set; } = DateTime.UtcNow;
        public string Result { get; set; } = "completed"; // completed, rejected, cancelled, missed
        public string? EndReason { get; set; }

        public int DurationSeconds
        {
            get
            {
                var end = EndedAt == default ? DateTime.UtcNow : EndedAt;
                var span = end - StartedAt;
                return span.TotalSeconds < 0 ? 0 : (int)Math.Round(span.TotalSeconds);
            }
        }
    }
}