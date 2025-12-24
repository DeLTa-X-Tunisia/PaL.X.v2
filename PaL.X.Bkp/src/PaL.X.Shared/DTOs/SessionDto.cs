using System;
using PaL.X.Shared.Enums;

namespace PaL.X.Shared.DTOs
{
    public class SessionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? DeviceSerial { get; set; }
        public DateTime ConnectedAt { get; set; }
        public DateTime LastActivity { get; set; }
        public UserStatus DisplayedStatus { get; set; }
        public UserStatus RealStatus { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateSessionRequest
    {
        public string Username { get; set; } = string.Empty;
        public string? DeviceSerial { get; set; }
    }

    public class UpdateStatusRequest
    {
        public UserStatus NewStatus { get; set; }
    }
}
