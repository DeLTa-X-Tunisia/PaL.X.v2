namespace PaL.X.Shared;

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsMe { get; set; }
    public string SenderName { get; set; } = string.Empty;
}
