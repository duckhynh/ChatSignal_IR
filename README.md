# Student Management System — Chat App (Teams-like)

Hướng dẫn chạy dự án (A → Z) — Razor Pages, .NET 8, SignalR, chunked file upload

Ứng dụng cung cấp giao diện chat, hỗ trợ phòng chat, emoji, upload file lớn (chunked) và preview ảnh.

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

Ứng dụng sẽ tự tạo `wwwroot/uploads/temp` và `wwwroot/uploads/files` khi khởi động. Nếu cần tạo thủ công:

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

- Tạo phòng: trang chính → nhập tên phòng + tên người tạo → Create
- Tham gia phòng: nhập username và roomId hoặc click phòng trên list
- Gửi tin nhắn: Enter để gửi, Shift+Enter xuống dòng
- Emoji: click nút emoji rồi chọn
- Upload file: click paperclip → chọn file (hỗ trợ file lớn, up tới 1GB bằng chunk)
- Xem ảnh: click thumbnail để mở modal
- Thu hồi tin nhắn: chọn context menu → Recall (hiển thị "Message has been recalled")

----
## 8. Cấu hình quan trọng

- Chunk size client: `Pages/Chat.cshtml` → `CHUNK_SIZE = 5 * 1024 * 1024` (5MB)
- Kestrel max request size: `Program.cs` cấu hình để cho phép upload đến 1GB
- File lưu tạm ở `wwwroot/uploads/temp`, file hoàn chỉnh ở `wwwroot/uploads/files` và metadata lưu trong DB

----
## 9. Các endpoint chính

- Rooms API: `/api/rooms`
- File upload init: `POST /api/fileupload/init`
- Upload chunk: `POST /api/fileupload/chunk/{sessionId}?chunkIndex=N`
- Complete upload: `POST /api/fileupload/complete/{sessionId}`
- File download: `GET /files/{fileId}`
- File preview: `GET /files/{fileId}/preview`
- File info: `GET /files/{fileId}/info`
- SignalR hub: `/chathub` (JoinRoom, LeaveRoom, SendMessage, SendFileMessage)

----
## 10. Troubleshooting (thường gặp)

- "Image not found":
  - Mở DevTools → Network → kiểm tra request `/files/{id}/preview` (status 404 hoặc 500)
  - Kiểm tra log server (FileDownloadController) sẽ in đường dẫn tìm file. Kiểm tra `wwwroot/uploads/files` có file tương ứng không.
  - Nếu `StoragePath` trong DB dùng dấu `/` trên Windows, migrations đã có sẵn để chuẩn hóa, nhưng bạn có thể cần sửa thủ công nếu gặp trường hợp cũ.

- Chunk upload IO exceptions (file lock):
  - Xóa các file `.tmp` trong `wwwroot/uploads/temp`, thử upload lại
  - Đảm bảo process có quyền ghi vào folder `wwwroot/uploads`

- EF migration errors:
  - Kiểm tra connection string, quyền tạo database

----
## 11. Lệnh hay dùng

- Build & run: `dotnet run --project Student_Management_System`
- Migration add: `dotnet ef migrations add Name --project Student_Management_System`
- Update db: `dotnet ef database update --project Student_Management_System`

----
## 12. Vị trí file quan trọng

- `Program.cs` — cấu hình server, SignalR
- `Pages/Chat.cshtml` — UI + client JS
- `wwwroot/css/teams-theme.css` — theme / responsive
- `Hubs/ChatHub.cs` — SignalR hub
- `Services/FileUploadService.cs` — server-side chunk merge
- `Controllers/FileUploadController.cs` / `FileDownloadController.cs` — API

----

Nếu bạn muốn hướng dẫn chi tiết theo hệ điều hành (Windows / macOS / Linux) hoặc script tự động setup, cho biết OS và mình sẽ bổ sung.
