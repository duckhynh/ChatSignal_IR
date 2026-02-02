using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Management_System.Data;

namespace Student_Management_System.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly ChatDbContext _context;

    public RoomsController(ChatDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetRooms()
    {
        var rooms = await _context.Rooms
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new 
            {
                r.Id,
                r.RoomId,
                r.Name,
                r.CreatedBy,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Room name is required" });
        }

        // Check for duplicate room name
        var existingRoom = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Name.ToLower() == request.Name.Trim().ToLower());
        
        if (existingRoom != null)
        {
            return Conflict(new { error = "A room with this name already exists" });
        }

        var roomId = Guid.NewGuid().ToString("N")[..8];

        var room = new Models.ChatRoom
        {
            RoomId = roomId,
            Name = request.Name.Trim(),
            CreatedBy = request.CreatedBy ?? "Anonymous",
            CreatedAt = DateTime.UtcNow
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        return Ok(new { room.Id, room.RoomId, room.Name, room.CreatedBy });
    }

    [HttpGet("{roomId}")]
    public async Task<IActionResult> GetRoom(string roomId)
    {
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
        if (room == null)
        {
            return NotFound();
        }

        return Ok(new 
        { 
            room.Id, 
            room.RoomId, 
            room.Name, 
            room.CreatedBy, 
            room.CreatedAt 
        });
    }

    [HttpDelete("{roomId}")]
    public async Task<IActionResult> DeleteRoom(string roomId, [FromQuery] string username)
    {
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
        if (room == null)
        {
            return NotFound("Room not found");
        }

        // Only the creator can delete the room
        if (!string.Equals(room.CreatedBy, username, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid("Only the room creator can delete this room");
        }

        // Delete all messages in the room
        var messages = await _context.Messages.Where(m => m.RoomId == roomId).ToListAsync();
        _context.Messages.RemoveRange(messages);

        // Delete the room
        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Room deleted successfully" });
    }
}

public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
}
