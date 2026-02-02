using Microsoft.AspNetCore.Mvc;
using Student_Management_System.Data;
using Student_Management_System.Services;

namespace Student_Management_System.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileUploadController : ControllerBase
{
    private readonly IFileUploadService _uploadService;
    private readonly ChatDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public FileUploadController(IFileUploadService uploadService, ChatDbContext context, IWebHostEnvironment environment)
    {
        _uploadService = uploadService;
        _context = context;
        _environment = environment;
    }

    [HttpPost("init")]
    public async Task<IActionResult> InitializeUpload([FromBody] InitUploadRequest request)
    {
        if (string.IsNullOrEmpty(request.FileName) || request.TotalSize <= 0)
        {
            return BadRequest("Invalid file information");
        }

        var sessionId = await _uploadService.InitializeUpload(
            request.FileName, 
            request.TotalSize, 
            request.ContentType ?? "application/octet-stream");

        return Ok(new { sessionId });
    }

    [HttpPost("chunk/{sessionId}")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB per chunk
    public async Task<IActionResult> UploadChunk(string sessionId, [FromQuery] int chunkIndex)
    {
        var session = _uploadService.GetSession(sessionId);
        if (session == null)
        {
            return NotFound("Upload session not found");
        }

        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms);
        var chunk = ms.ToArray();

        var success = await _uploadService.UploadChunk(sessionId, chunk, chunkIndex);
        if (!success)
        {
            return StatusCode(500, "Failed to upload chunk");
        }

        var progress = (int)((session.UploadedSize + chunk.Length) * 100 / session.TotalSize);
        return Ok(new { progress, uploadedSize = session.UploadedSize + chunk.Length });
    }

    [HttpPost("complete/{sessionId}")]
    public async Task<IActionResult> CompleteUpload(string sessionId)
    {
        var fileId = await _uploadService.CompleteUpload(sessionId);
        if (fileId == null)
        {
            return StatusCode(500, "Failed to complete upload");
        }

        return Ok(new { fileId });
    }

    [HttpGet("progress/{sessionId}")]
    public IActionResult GetProgress(string sessionId)
    {
        var session = _uploadService.GetSession(sessionId);
        if (session == null)
        {
            return NotFound("Upload session not found");
        }

        var progress = session.TotalSize > 0 
            ? (int)(session.UploadedSize * 100 / session.TotalSize) 
            : 0;

        return Ok(new { progress, uploadedSize = session.UploadedSize, totalSize = session.TotalSize });
    }
}

public class InitUploadRequest
{
    public string FileName { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public string? ContentType { get; set; }
}
