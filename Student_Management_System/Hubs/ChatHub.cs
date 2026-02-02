using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Student_Management_System.Data;
using Student_Management_System.Models;

namespace Student_Management_System.Hubs;

public class ChatHub : Hub
{
    private readonly ChatDbContext _context;
    private static readonly Dictionary<string, HashSet<string>> RoomUsers = new();

    public ChatHub(ChatDbContext context)
    {
        _context = context;
    }

    public async Task JoinRoom(string roomId, string username)
    {
        // Check if username already exists in the room
        lock (RoomUsers)
        {
            if (RoomUsers.ContainsKey(roomId) && 
                RoomUsers[roomId].Any(u => u.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                // Username already exists - notify caller
                Clients.Caller.SendAsync("UsernameExists", username, roomId);
                return;
            }
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        lock (RoomUsers)
        {
            if (!RoomUsers.ContainsKey(roomId))
            {
                RoomUsers[roomId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            RoomUsers[roomId].Add(username);
        }

        // Get room info
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
        var roomName = room?.Name ?? roomId;
        var createdBy = room?.CreatedBy ?? "";

        await Clients.Group(roomId).SendAsync("UserJoined", username, roomId);
        await Clients.Caller.SendAsync("RoomJoined", roomId, roomName, createdBy, GetRoomUsers(roomId));

        // Load recent messages
        var recentMessages = await _context.Messages
            .Include(m => m.File)
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.SentAt)
            .Take(50)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        await Clients.Caller.SendAsync("LoadMessages", recentMessages.Select(m => new
        {
            m.Id,
            m.RoomId,
            m.Sender,
            Type = m.Type.ToString(),
            m.Content,
            m.SentAt,
            m.FileId,
            m.IsRecalled,
            FileName = m.File?.FileName,
            FileSize = m.File?.Size,
            FileContentType = m.File?.ContentType
        }));
    }

    public async Task LeaveRoom(string roomId, string username)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

        lock (RoomUsers)
        {
            if (RoomUsers.ContainsKey(roomId))
            {
                RoomUsers[roomId].Remove(username);
            }
        }

        await Clients.Group(roomId).SendAsync("UserLeft", username, roomId);
    }

    // Typing indicator methods
    public async Task StartTyping(string roomId, string username)
    {
        await Clients.OthersInGroup(roomId).SendAsync("UserStartedTyping", username);
    }

    public async Task StopTyping(string roomId, string username)
    {
        await Clients.OthersInGroup(roomId).SendAsync("UserStoppedTyping", username);
    }



    public async Task SendMessage(string roomId, string sender, string content, string messageType)
    {
        var type = Enum.Parse<MessageType>(messageType, true);

        var message = new ChatMessage
        {
            RoomId = roomId,
            Sender = sender,
            Content = content,
            Type = type,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        await Clients.Group(roomId).SendAsync("ReceiveMessage", new
        {
            message.Id,
            message.RoomId,
            message.Sender,
            Type = message.Type.ToString(),
            message.Content,
            message.SentAt,
            message.FileId,
            message.IsRecalled
        });
    }


    public async Task SendFileMessage(string roomId, string sender, int fileId, string messageType)
    {
        var file = await _context.Files.FindAsync(fileId);
        if (file == null) return;

        var type = Enum.Parse<MessageType>(messageType, true);

        var message = new ChatMessage
        {
            RoomId = roomId,
            Sender = sender,
            Content = file.FileName,
            Type = type,
            SentAt = DateTime.UtcNow,
            FileId = fileId
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        await Clients.Group(roomId).SendAsync("ReceiveMessage", new
        {
            message.Id,
            message.RoomId,
            message.Sender,
            Type = message.Type.ToString(),
            message.Content,
            message.SentAt,
            message.FileId,
            message.IsRecalled,
            FileName = file.FileName,
            FileSize = file.Size,
            FileContentType = file.ContentType
        });
    }

    public async Task NotifyFileUploadProgress(string roomId, string sender, string fileName, int progress)
    {
        await Clients.Group(roomId).SendAsync("FileUploadProgress", sender, fileName, progress);
    }

    public async Task NotifyRoomDeleted(string roomId)
    {
        await Clients.Group(roomId).SendAsync("RoomDeleted", roomId);
        
        // Clear room users
        lock (RoomUsers)
        {
            if (RoomUsers.ContainsKey(roomId))
            {
                RoomUsers.Remove(roomId);
            }
        }
    }

    public async Task RecallMessage(string roomId, int messageId, string sender)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message != null && message.Sender == sender && message.RoomId == roomId && !message.IsRecalled)
        {
            message.IsRecalled = true;
            await _context.SaveChangesAsync();
            
            await Clients.Group(roomId).SendAsync("MessageRecalled", messageId, sender, message.Type.ToString());
        }
    }

    private List<string> GetRoomUsers(string roomId)
    {
        lock (RoomUsers)
        {
            return RoomUsers.ContainsKey(roomId) ? RoomUsers[roomId].ToList() : new List<string>();
        }
    }

    public Task<List<string>> GetRoomUsersAsync(string roomId)
    {
        return Task.FromResult(GetRoomUsers(roomId));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
