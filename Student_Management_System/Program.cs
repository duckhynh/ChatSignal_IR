using Microsoft.EntityFrameworkCore;
using Student_Management_System.Data;
using Student_Management_System.Hubs;
using Student_Management_System.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB for SignalR messages
});

// Configure EF Core with SQL Server
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

// Configure request size for large file uploads (1GB)
// C?u hình Kestrel ?? listen trên t?t c? các network interfaces (0.0.0.0)
// Cho phép các máy trong cùng m?ng LAN k?t n?i ???c
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024L * 1024 * 1024; // 1GB per request
    options.Limits.MaxRequestLineSize = 16 * 1024; // 16KB
    options.Limits.MaxRequestHeadersTotalSize = 64 * 1024; // 64KB
});

// C?u hình URLs - h? tr? c? localhost và LAN
// 0.0.0.0 = listen trên t?t c? network interfaces (localhost + IP LAN)
builder.WebHost.UseUrls("http://0.0.0.0:5000", "https://0.0.0.0:5001");

var app = builder.Build();

// Ensure upload directories exist
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
var tempPath = Path.Combine(uploadsPath, "temp");
var filesPath = Path.Combine(uploadsPath, "files");

Directory.CreateDirectory(tempPath);
Directory.CreateDirectory(filesPath);

app.Logger.LogInformation("Upload directories verified at {UploadsPath}", uploadsPath);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHub<ChatHub>("/chathub");

app.Run();

