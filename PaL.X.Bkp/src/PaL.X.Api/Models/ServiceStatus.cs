namespace PaL.X.Api.Models
{
    public class ServiceStatus
    {
        public bool IsRunning { get; set; }
        public bool IsPublicChatEnabled { get; set; }
        public DateTime? StartedAt { get; set; }
        public int ConnectedClients { get; set; }
        public List<ClientInfo> Clients { get; set; } = new List<ClientInfo>();
    }

    public class ClientInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
    }
}