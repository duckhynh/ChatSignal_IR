namespace Student_Management_System.Models;

public enum MessageType
{
    Text,
    Icon,
    Image,
    File
}

public class ChatMessage
{
    public int Id { get; set; }
    public string RoomId { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public int? FileId { get; set; }
    public bool IsRecalled { get; set; } = false;


    public ChatFile? File { get; set; }
}
