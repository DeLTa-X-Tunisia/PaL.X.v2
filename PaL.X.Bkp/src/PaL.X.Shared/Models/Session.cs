using System;
using PaL.X.Shared.Enums;

namespace PaL.X.Shared.Models
{
    public class Session
    {
        public int Id { get; set; }
        
        // User reference
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        
        // Connection info
        public string Username { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? DeviceSerial { get; set; }
        
        // Session timestamps
        public DateTime ConnectedAt { get; set; }
        public DateTime LastActivity { get; set; }
        public DateTime? DisconnectedAt { get; set; }
        
        // Status management
        public UserStatus DisplayedStatus { get; set; } = UserStatus.Online;
        public UserStatus RealStatus { get; set; } = UserStatus.Online;
        
        // Session state
        public bool IsActive { get; set; } = true;
    }
}
