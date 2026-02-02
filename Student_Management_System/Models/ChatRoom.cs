namespace Student_Management_System.Models;

public class ChatRoom
{
    public int Id { get; set; }
    public string RoomId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
