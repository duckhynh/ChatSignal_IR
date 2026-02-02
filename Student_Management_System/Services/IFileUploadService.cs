namespace Student_Management_System.Services;

public class FileUploadSession
{
    public string SessionId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string TempPath { get; set; } = string.Empty;
    public long UploadedSize { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface IFileUploadService
{
    Task<string> InitializeUpload(string fileName, long totalSize, string contentType);
    Task<bool> UploadChunk(string sessionId, byte[] chunk, int chunkIndex);
    Task<int?> CompleteUpload(string sessionId);
    FileUploadSession? GetSession(string sessionId);
}
