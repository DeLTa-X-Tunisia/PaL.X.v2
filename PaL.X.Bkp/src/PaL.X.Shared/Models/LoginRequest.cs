namespace PaL.X.Shared.Models
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool ConnectOffline { get; set; }
        public string? DeviceSerial { get; set; }
    }
}