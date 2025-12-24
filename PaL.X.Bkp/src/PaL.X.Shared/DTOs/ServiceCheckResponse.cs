namespace PaL.X.Shared.DTOs
{
    public class ServiceCheckResponse
    {
        public bool ServiceAvailable { get; set; }
        public bool ClientValid { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}