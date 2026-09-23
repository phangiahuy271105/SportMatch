# Hướng dẫn chạy SportMatch

## 1. Chuẩn bị môi trường

Cài các thành phần sau:

- Windows 10 hoặc Windows 11.
- .NET 10 SDK.
- SQL Server Express LocalDB.
- Visual Studio 2022 trở lên nếu muốn chạy bằng giao diện IDE.

Kiểm tra nhanh trong PowerShell:

```powershell
dotnet --version
sqllocaldb info MSSQLLocalDB
```

## 2. Lấy source

```powershell
git clone https://github.com/phangiahuy271105/SportMatch.git
cd SportMatch
dotnet restore
```

Nếu nhận source dưới dạng ZIP, hãy giải nén toàn bộ rồi mở terminal tại thư mục chứa `SportMatch.sln`.

## 3. Tạo mật khẩu admin cục bộ

Chạy lệnh sau tại thư mục gốc dự án:

```powershell
dotnet user-secrets set "AdminBootstrap:Password" "SportMatch@123" --project SportMatch.Web
```

Có thể thay mật khẩu ví dụ bằng mật khẩu khác. Mật khẩu phải có ít nhất 10 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt. User secret được lưu ngoài repository nên không bị đẩy lên GitHub.

## 4. Chạy ứng dụng

### Cách A — terminal

```powershell
dotnet run --project SportMatch.Web --launch-profile https
```

Mở các địa chỉ:

- Website: `https://localhost:7026`
- Quản trị: `https://localhost:7026/Admin/Login`
- Health check: `https://localhost:7026/health`

Đăng nhập bằng email `admin@sportmatch.vn` và mật khẩu đã cấu hình ở bước 3.

### Cách B — Visual Studio

1. Mở `SportMatch.sln`.
2. Chọn `SportMatch.Web` làm Startup Project.
3. Chọn profile `https`.
4. Nhấn `F5`.

## 5. Điều gì xảy ra ở lần chạy đầu

Ứng dụng tự động:

1. Kết nối tới `(localdb)\MSSQLLocalDB`.
2. Tạo database `SportMatchDb` nếu chưa tồn tại.
3. Chạy toàn bộ Entity Framework migrations.
4. Tạo tài khoản admin nếu mật khẩu đã được cấu hình.
5. Tạo 3 cụm sân, 7 sân con, các khung giờ, 3 kèo mẫu và liên kết ảnh demo.

Không cần tải hoặc restore file `.bak`. Nếu database đã có cụm sân, dữ liệu demo sẽ không được tạo lại; dữ liệu và ảnh hiện có được giữ nguyên.

## 6. Chạy kiểm tra

```powershell
dotnet build SportMatch.sln
dotnet run --project tests/SportMatch.SmokeTests
```

Kết quả mong đợi của smoke test là `SportMatch smoke tests: 10/10 passed.`

## 7. Xử lý lỗi thường gặp

### LocalDB không tồn tại

Cài SQL Server Express LocalDB hoặc bổ sung workload **Data storage and processing** trong Visual Studio Installer.

### LocalDB không khởi động

Kiểm tra và thử khởi động:

```powershell
sqllocaldb info MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

Nếu SQL Server vẫn báo `SQL Server process failed to start`, hãy repair/cài lại LocalDB. Không xóa instance khi máy đang có database cần giữ mà chưa backup.

### Trình duyệt cảnh báo chứng chỉ HTTPS

```powershell
dotnet dev-certs https --trust
```

Sau đó đóng trình duyệt, chạy lại ứng dụng và mở `https://localhost:7026`.

### Không đăng nhập được admin

- Xác nhận đã chạy lệnh `dotnet user-secrets set` đúng project.
- Mật khẩu phải thỏa chính sách ở bước 3.
- Tài khoản chỉ được bootstrap khi email admin chưa tồn tại.
- Kiểm tra log lúc khởi động; ứng dụng sẽ báo rõ nếu tạo tài khoản hoặc gán quyền thất bại.

## 8. Dữ liệu ảnh

- Ảnh demo cố định: `SportMatch.Web/wwwroot/images/demo` — được đưa lên Git.
- Ảnh admin tải lên: `SportMatch.Web/wwwroot/uploads` — chỉ tồn tại cục bộ và bị `.gitignore` loại bỏ.
- Muốn chuyển ảnh admin sang máy khác, cần sao chép riêng thư mục `uploads`.
