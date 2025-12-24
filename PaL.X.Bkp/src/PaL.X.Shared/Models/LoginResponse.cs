using PaL.X.Shared.DTOs;

namespace PaL.X.Shared.Models
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserData? User { get; set; }
    }
}