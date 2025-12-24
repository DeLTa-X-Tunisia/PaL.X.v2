using System;
using System.ComponentModel.DataAnnotations;
using PaL.X.Shared.Enums;

namespace PaL.X.Shared.Models;

public class Session
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Country { get; set; }
    public string? DeviceSerial { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public DateTime? DisconnectedAt { get; set; }
    public UserStatus DisplayedStatus { get; set; } = UserStatus.Online;
    public UserStatus RealStatus { get; set; } = UserStatus.Online;
    public bool IsActive { get; set; } = true;
}
