# SportMatch

SportMatch là website ASP.NET Core MVC hỗ trợ tìm và đặt sân thể thao, thanh toán cọc bằng VietQR, tìm người chơi cùng và quản trị vận hành sân.

## Chức năng chính

- Tìm sân theo khu vực, môn thể thao và ngày chơi.
- Đặt nhiều khung giờ liên tiếp, giữ chỗ và tạo VietQR tiền cọc.
- Tra cứu lịch đặt bằng mã booking và số điện thoại.
- Mở kèo từ booking hoặc tạo kèo độc lập; duyệt người tham gia và xác nhận cọc.
- Quản trị cụm sân, sân con, ảnh, khung giờ, booking, khách hàng và hoàn tiền.
- Ghi nhật ký thao tác quản trị, giới hạn tần suất request và bảo vệ các luồng bằng token.

## Công nghệ

- ASP.NET Core MVC trên .NET 10
- Entity Framework Core 10
- SQL Server Express LocalDB
- ASP.NET Core Identity
- HTML, CSS và JavaScript thuần

## Chạy nhanh

### Yêu cầu

- Windows 10/11.
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- SQL Server Express LocalDB. Có thể cài cùng workload **ASP.NET and web development** hoặc **Data storage and processing** của Visual Studio.
- Visual Studio 2022 trở lên là tùy chọn; có thể chạy hoàn toàn bằng terminal.

### Bằng terminal

```powershell
git clone https://github.com/phangiahuy271105/SportMatch.git
cd SportMatch
dotnet restore
dotnet user-secrets set "AdminBootstrap:Password" "SportMatch@123" --project SportMatch.Web
dotnet run --project SportMatch.Web --launch-profile https
```

Mở `https://localhost:7026`. Trang quản trị nằm tại `https://localhost:7026/Admin/Login`:

- Email: `admin@sportmatch.vn`
- Mật khẩu: giá trị đã đặt trong user secrets

Mật khẩu admin cần ít nhất 10 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.

### Bằng Visual Studio

1. Clone repository và mở `SportMatch.sln`.
2. Mở **View → Terminal**, chạy lệnh cấu hình user secret ở trên.
3. Chọn `SportMatch.Web` làm Startup Project.
4. Chọn profile `https` và nhấn `F5`.

Xem hướng dẫn chi tiết và xử lý lỗi tại [HUONG-DAN-CHAY.md](HUONG-DAN-CHAY.md).

## Database và dữ liệu demo

Ở lần chạy đầu, ứng dụng tự tạo `SportMatchDb`, chạy migrations và seed:

- 3 cụm sân, 7 sân con và khung giờ từ 06:00 đến 23:00.
- 3 kèo mẫu.
- 7 ảnh demo cố định trong `SportMatch.Web/wwwroot/images/demo`.
- Tài khoản admin nếu đã cấu hình mật khẩu.

Không cần file `.bak`. Nếu database đã có ít nhất một cụm sân, ứng dụng không seed lại dữ liệu demo và không ghi đè ảnh do admin tải lên.

## Cấu hình

Cấu hình mặc định nằm trong `SportMatch.Web/appsettings.json`:

- `ConnectionStrings:SportMatchDb`: kết nối SQL Server.
- `AdminBootstrap`: tài khoản admin được tạo ở lần chạy đầu.
- `Payment`: ngân hàng, tỷ lệ cọc, thời gian giữ chỗ, webhook và Zalo.

Không commit mật khẩu hoặc API key. Dùng .NET user secrets khi phát triển và biến môi trường khi triển khai.

## Kiểm tra source

```powershell
dotnet build SportMatch.sln
dotnet run --project tests/SportMatch.SmokeTests
```

Xem [DEMO-CHECKLIST.md](DEMO-CHECKLIST.md) để kiểm tra các luồng trước khi trình diễn.

## Dữ liệu không đưa lên Git

Các thư mục/file sau đã được loại bằng `.gitignore`:

- Database và backup: `*.mdf`, `*.ldf`, `*.bak`.
- Ảnh do admin tải lên: `SportMatch.Web/wwwroot/uploads`.
- Khóa Data Protection: `SportMatch.Web/App_Data`.
- Kết quả build và cấu hình máy cá nhân: `bin`, `obj`, `.vs`, `.artifacts`.

Ảnh trong thư mục `images/demo` là một phần của source và cần được commit.
