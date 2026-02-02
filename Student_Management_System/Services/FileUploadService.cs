using Student_Management_System.Data;
using Student_Management_System.Models;
using System.Collections.Concurrent;

namespace Student_Management_System.Services;

public class FileUploadService : IFileUploadService
{
    private readonly ChatDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileUploadService> _logger;
    private static readonly ConcurrentDictionary<string, FileUploadSession> Sessions = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> SessionLocks = new();

    public FileUploadService(ChatDbContext context, IWebHostEnvironment environment, ILogger<FileUploadService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public Task<string> InitializeUpload(string fileName, long totalSize, string contentType)
    {
        var sessionId = Guid.NewGuid().ToString();
        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "temp");
        Directory.CreateDirectory(uploadsPath);

        var tempPath = Path.Combine(uploadsPath, $"{sessionId}.tmp");

        var session = new FileUploadSession
        {
            SessionId = sessionId,
            FileName = fileName,
            TotalSize = totalSize,
            ContentType = contentType,
            TempPath = tempPath,
            UploadedSize = 0,
            CreatedAt = DateTime.UtcNow
        };

        Sessions[sessionId] = session;
        SessionLocks[sessionId] = new SemaphoreSlim(1, 1);

        // Create empty file - DO NOT use SetLength (causes file corruption when appending)
        File.WriteAllBytes(tempPath, Array.Empty<byte>());

        _logger.LogInformation("Initialized upload session {SessionId} for file {FileName}, size {Size:N0} bytes", 
            sessionId, fileName, totalSize);

        return Task.FromResult(sessionId);
    }

    public async Task<bool> UploadChunk(string sessionId, byte[] chunk, int chunkIndex)
    {
        if (!Sessions.TryGetValue(sessionId, out var session))
        {
            _logger.LogWarning("Session {SessionId} not found", sessionId);
            return false;
        }

        if (!SessionLocks.TryGetValue(sessionId, out var semaphore))
        {
            _logger.LogWarning("Lock for session {SessionId} not found", sessionId);
            return false;
        }

        // Wait for lock with timeout (30 seconds)
        if (!await semaphore.WaitAsync(TimeSpan.FromSeconds(30)))
        {
            _logger.LogError("Timeout waiting for lock for session {SessionId}, chunk {ChunkIndex}", sessionId, chunkIndex);
            return false;
        }

        try
        {
            // Retry mechanism for file access
            const int maxRetries = 3;
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    // Use FileMode.Append - simple and correct
                    await using var fs = new FileStream(
                        session.TempPath, 
                        FileMode.Append,  // APPEND mode - no Seek needed
                        FileAccess.Write, 
                        FileShare.None,   // Exclusive access
                        bufferSize: 65536,
                        useAsync: true);
                    
                    await fs.WriteAsync(chunk);
                    await fs.FlushAsync();

                    session.UploadedSize += chunk.Length;

                    var progress = session.TotalSize > 0 ? (session.UploadedSize * 100) / session.TotalSize : 0;
                    _logger.LogDebug("Uploaded chunk {ChunkIndex} for session {SessionId}, progress: {Progress}%", 
                        chunkIndex, sessionId, progress);

                    return true;
                }
                catch (IOException ioEx) when (retry < maxRetries - 1)
                {
                    _logger.LogWarning(ioEx, "IO error on chunk {ChunkIndex} for session {SessionId}, retry {Retry}", 
                        chunkIndex, sessionId, retry + 1);
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * (retry + 1))); // Exponential backoff
                }
            }

            _logger.LogError("Failed to upload chunk {ChunkIndex} for session {SessionId} after {MaxRetries} retries", 
                chunkIndex, sessionId, maxRetries);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading chunk {ChunkIndex} for session {SessionId}", chunkIndex, sessionId);
            return false;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<int?> CompleteUpload(string sessionId)
    {
        if (!Sessions.TryRemove(sessionId, out var session))
        {
            _logger.LogWarning("Session {SessionId} not found for completion", sessionId);
            return null;
        }

        // Remove and dispose lock
        if (SessionLocks.TryRemove(sessionId, out var semaphore))
        {
            semaphore.Dispose();
        }

        try
        {
            // Wait a bit to ensure all file handles are released
            await Task.Delay(100);

            var finalPath = Path.Combine(_environment.WebRootPath, "uploads", "files");
            Directory.CreateDirectory(finalPath);

            var fileExtension = Path.GetExtension(session.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var destinationPath = Path.Combine(finalPath, uniqueFileName);

            // Verify temp file exists
            if (!File.Exists(session.TempPath))
            {
                _logger.LogError("Temp file not found: {TempPath}", session.TempPath);
                return null;
            }

            var tempFileInfo = new FileInfo(session.TempPath);
            _logger.LogInformation("Moving temp file: {TempPath} (actual size: {ActualSize} bytes) to {DestPath}", 
                session.TempPath, tempFileInfo.Length, destinationPath);

            // Move file (simple, no retry needed for small files)
            File.Move(session.TempPath, destinationPath, overwrite: true);

            // Save to database with normalized path and ACTUAL file size
            var chatFile = new ChatFile
            {
                FileName = session.FileName,
                Size = tempFileInfo.Length,  // Use actual file size, not expected size
                ContentType = session.ContentType,
                StoragePath = Path.Combine("uploads", "files", uniqueFileName),
                UploadedAt = DateTime.UtcNow
            };

            _context.Files.Add(chatFile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Completed upload for session {SessionId}, FileId: {FileId}, Size: {Size:N0} bytes", 
                sessionId, chatFile.FileId, session.TotalSize);

            return chatFile.FileId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing upload for session {SessionId}", sessionId);
            
            // Cleanup temp file
            try
            {
                if (File.Exists(session.TempPath))
                {
                    await Task.Delay(500);
                    File.Delete(session.TempPath);
                }
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to cleanup temp file for session {SessionId}", sessionId);
            }
            
            return null;
        }
    }

    public FileUploadSession? GetSession(string sessionId)
    {
        Sessions.TryGetValue(sessionId, out var session);
        return session;
    }
}
