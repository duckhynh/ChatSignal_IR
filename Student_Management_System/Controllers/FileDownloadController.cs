using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Management_System.Data;

namespace Student_Management_System.Controllers;

[ApiController]
[Route("files")]
public class FileDownloadController : ControllerBase
{
    private readonly ChatDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileDownloadController> _logger;

    public FileDownloadController(ChatDbContext context, IWebHostEnvironment environment, ILogger<FileDownloadController> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("{fileId}")]
    public async Task<IActionResult> DownloadFile(int fileId)
    {
        var file = await _context.Files.FindAsync(fileId);
        if (file == null)
        {
            _logger.LogWarning("File {FileId} not found in database", fileId);
            return NotFound("File not found");
        }

        var filePath = Path.Combine(_environment.WebRootPath, file.StoragePath);
        _logger.LogInformation("Looking for file at: {FilePath}", filePath);
        
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogError("File {FileId} not found on disk at {FilePath}", fileId, filePath);
            return NotFound($"File not found on disk: {file.StoragePath}");
        }

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, file.ContentType, file.FileName);
    }

    [HttpGet("{fileId}/preview")]
    public async Task<IActionResult> PreviewFile(int fileId)
    {
        var file = await _context.Files.FindAsync(fileId);
        if (file == null)
        {
            _logger.LogWarning("File {FileId} not found in database for preview", fileId);
            return NotFound("File not found");
        }

        var filePath = Path.Combine(_environment.WebRootPath, file.StoragePath);
        _logger.LogInformation("Preview - Looking for file at: {FilePath}", filePath);
        
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogError("File {FileId} not found on disk at {FilePath} for preview", fileId, filePath);
            return NotFound($"File not found on disk: {file.StoragePath}");
        }

        // Only allow preview for images
        if (!file.ContentType.StartsWith("image/"))
        {
            _logger.LogWarning("Non-image file {FileId} requested for preview", fileId);
            return BadRequest("Preview only available for images");
        }

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, file.ContentType);
    }

    [HttpGet("{fileId}/info")]
    public async Task<IActionResult> GetFileInfo(int fileId)
    {
        var file = await _context.Files.FindAsync(fileId);
        if (file == null)
        {
            return NotFound("File not found");
        }

        var fullPath = Path.Combine(_environment.WebRootPath, file.StoragePath);
        var fileExists = System.IO.File.Exists(fullPath);

        return Ok(new
        {
            file.FileId,
            file.FileName,
            file.Size,
            file.ContentType,
            file.UploadedAt,
            file.StoragePath,
            FullPath = fullPath,
            FileExists = fileExists,
            WebRootPath = _environment.WebRootPath,
            DirectoryExists = Directory.Exists(Path.GetDirectoryName(fullPath))
        });
    }
}

