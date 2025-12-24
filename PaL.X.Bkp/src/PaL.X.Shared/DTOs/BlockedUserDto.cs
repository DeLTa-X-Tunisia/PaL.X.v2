using System;

namespace PaL.X.Shared.DTOs
{
    public class BlockedUserDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BlockedByUserId { get; set; }
        public DateTime BlockedOn { get; set; }
        public DateTime? BlockedUntil { get; set; }
        public int? DurationDays { get; set; }
        public bool IsPermanent { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
