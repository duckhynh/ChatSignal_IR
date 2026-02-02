# Student Management System — Chat App (Teams-like)

Hướng dẫn chạy dự án (A → Z) — Razor Pages, .NET 8, SignalR, chunked file upload

Ứng dụng cung cấp giao diện chat, hỗ trợ phòng chat, emoji, upload file lớn (chunked) và preview ảnh.

**Theme:** Light (Windows 11 Blue) + Dark (Pure Black/Jet Black)  
**Features:** Real-time chat, file upload, typing indicator, message recall, responsive design

----
## 1. Yêu cầu môi trường

- .NET 8 SDK
- SQL Server (LocalDB / Express / full) hoặc database tương thích
- (Tùy chọn) Visual Studio 2022/2023 hoặc VS Code

----
## 2. Chuẩn bị mã nguồn

1. Mở terminal và chuyển vào thư mục dự án:

```bash
cd Student_Management_System
```

2. Khôi phục và build:

```bash
dotnet restore
dotnet build
```

----
## 3. Cấu hình kết nối database

Mở `appsettings.json` hoặc `appsettings.Development.json` và đặt chuỗi kết nối `DefaultConnection`. Ví dụ LocalDB (Windows):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ChatAppDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

**Lưu ý:** `appsettings.Development.json` và `appsettings.*.local.json` bị ignore bởi .gitignore

----
## 4. Áp dụng migrations (tạo schema)

Nếu `dotnet-ef` chưa cài, chạy:

```bash
dotnet tool install --global dotnet-ef
```

Chạy migrations có sẵn:

```bash
dotnet ef database update --project Student_Management_System
```

Nếu bạn thay đổi model và cần tạo migration mới:

```bash
dotnet ef migrations add <TênMigration> --project Student_Management_System
dotnet ef database update --project Student_Management_System
```

----
## 5. Thư mục upload

Ứng dụng sẽ tự tạo `wwwroot/uploads/temp` và `wwwroot/uploads/files` khi khởi động. 

**Lưu ý:** Các thư mục này bị ignore bởi .gitignore (chỉ giữ lại `.gitkeep`)

Windows PowerShell:

```powershell
mkdir .\wwwroot\uploads\temp -Force
mkdir .\wwwroot\uploads\files -Force
```

Linux/macOS:

```bash
mkdir -p wwwroot/uploads/temp
mkdir -p wwwroot/uploads/files
```

----
## 6. Chạy ứng dụng

Từ terminal:

```bash
dotnet run --project Student_Management_System
```

Hoặc chạy từ Visual Studio (F5). Mở trình duyệt vào địa chỉ console hiển thị (ví dụ https://localhost:5001).

----
## 7. Hướng dẫn sử dụng nhanh

- **Tạo phòng:** trang chính → nhập tên phòng + tên người tạo → Create
- **Tham gia phòng:** nhập username và roomId hoặc click phòng trên list
- **Gửi tin nhắn:** Enter để gửi, Shift+Enter xuống dòng
- **Emoji:** click nút emoji rồi chọn
- **Upload file:** click paperclip → chọn file (hỗ trợ file lớn, up tới 1GB bằng chunk)
- **Xem ảnh:** click thumbnail để mở modal
- **Thu hồi tin nhắn:** chọn context menu → Recall (hiển thị "Message has been recalled")
- **Typing indicator:** bạn sẽ thấy "User is typing..." khi người khác gõ tin nhắn
- **Dark Mode:** click nút theme ở left nav để switch giữa Light/Dark

----
## 8. Cấu hình quan trọng

- **Chunk size client:** `Pages/Chat.cshtml` → `CHUNK_SIZE = 5 * 1024 * 1024` (5MB)
- **Kestrel max request size:** `Program.cs` cấu hình để cho phép upload đến 1GB
- **File storage:** 
  - Tạm: `wwwroot/uploads/temp` 
  - Hoàn chỉnh: `wwwroot/uploads/files`
  - Metadata: Database
- **Dark mode:** pure black background (#0a0a0a), bright cyan accent (#00D9FF)
- **Typing timeout:** 2 giây không gõ sẽ tự động stop typing indicator

----
## 9. Các endpoint chính

- Rooms API: `/api/rooms`
- File upload init: `POST /api/fileupload/init`
- Upload chunk: `POST /api/fileupload/chunk/{sessionId}?chunkIndex=N`
- Complete upload: `POST /api/fileupload/complete/{sessionId}`
- File download: `GET /files/{fileId}`
- File preview: `GET /files/{fileId}/preview`
- File info: `GET /files/{fileId}/info`
- SignalR hub: `/chathub` (JoinRoom, LeaveRoom, SendMessage, SendFileMessage, StartTyping, StopTyping)

----
## 10. .gitignore - Những file bị ignore

```
# Build & Compiler
bin/, obj/, log/

# Visual Studio
.vs/, *.suo, *.user

# IDE
.vscode/, .idea/, *.swp

# Configuration (Sensitive)
appsettings.Development.json, appsettings.*.local.json, .env

# Database
*.db, *.db-shm, *.db-wal

# File Uploads
wwwroot/uploads/temp/* (chỉ giữ .gitkeep)
wwwroot/uploads/files/* (chỉ giữ .gitkeep)

# OS Files
.DS_Store, Thumbs.db, Desktop.ini

# Packages
packages/, node_modules/
```

**Lưu ý:** Sử dụng `.gitkeep` để giữ lại thư mục trống cho Git

----
## 11. Troubleshooting (thường gặp)

## 11. Troubleshooting (thường gặp)

- **"Image not found":**
  - Mở DevTools → Network → kiểm tra request `/files/{id}/preview` (status 404 hoặc 500)
  - Kiểm tra log server (FileDownloadController) sẽ in đường dẫn tìm file
  - Kiểm tra `wwwroot/uploads/files` có file tương ứng không
  - Nếu `StoragePath` trong DB dùng dấu `/` trên Windows, migrations đã có sẵn để chuẩn hóa

- **Chunk upload IO exceptions (file lock):**
  - Xóa các file `.tmp` trong `wwwroot/uploads/temp`, thử upload lại
  - Đảm bảo process có quyền ghi vào folder `wwwroot/uploads`

- **EF migration errors:**
  - Kiểm tra connection string, quyền tạo database

- **Typing indicator không hiện:**
  - Kiểm tra console browser xem có lỗi không
  - Đảm bảo SignalR connection hoạt động (xem console: "Connected to chat")

- **Git commit file uploads:**
  - Các file upload sẽ bị ignore (check .gitignore)
  - Chỉ `.gitkeep` được track để giữ lại thư mục

----
## 12. Lệnh hay dùng

- Build & run: `dotnet run --project Student_Management_System`
- Migration add: `dotnet ef migrations add Name --project Student_Management_System`
- Update db: `dotnet ef database update --project Student_Management_System`
- Check git status: `git status` (sẽ bỏ qua upload files)

----
## 13. Vị trí file quan trọng

- `Program.cs` — cấu hình server, SignalR
- `Pages/Chat.cshtml` — UI + client JS
- `wwwroot/css/teams-theme.css` — theme (light + dark mode)
- `Hubs/ChatHub.cs` — SignalR hub
- `Services/FileUploadService.cs` — server-side chunk merge
- `Controllers/FileUploadController.cs` / `FileDownloadController.cs` — API
- `.gitignore` — ignore rules cho dự án

----
## 14. Theme Colors

### Light Mode (Windows 11 Style)
- Primary: #0078D4 (Microsoft Blue)
- Background: #F3F3F3
- Surface: #FFFFFF
- Text: #1A1A1A

### Dark Mode (Pure Black / Jet Black)
- Primary: #00D9FF (Bright Cyan)
- Background: #0a0a0a (Pure Black)
- Surface: #121212 (Dark Gray)
- Text: #FFFFFF
- Status Colors: Green (#00FF88), Yellow (#FFD700), Red (#FF4444)

----

Nếu bạn muốn hướng dẫn chi tiết theo hệ điều hành (Windows / macOS / Linux) hoặc script tự động setup, cho biết OS và mình sẽ bổ sung.
