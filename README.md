# SportMatch

Website ASP.NET Core MVC hỗ trợ đặt sân, thanh toán cọc bằng VietQR, cáp kèo và quản trị vận hành sân thể thao.

## Yêu cầu

- Visual Studio 2022 hoặc mới hơn, có ASP.NET and web development workload
- .NET 10 SDK
- SQL Server LocalDB (`MSSQLLocalDB`)

## Chạy dự án

1. Mở `SportMatch.sln`.
2. Chọn `SportMatch.Web` làm Startup Project.
3. Cấu hình mật khẩu admin bằng biến môi trường `AdminBootstrap__Password` trong lần chạy đầu tiên.
4. Nhấn `F5`. Database `SportMatchDb` và dữ liệu sân mẫu được tạo tự động.
5. Đăng nhập admin tại `/Account/Login` với email `admin@sportmatch.vn` và mật khẩu"SportMatch@123".

Không đưa mật khẩu, file database, thư mục upload, `bin`, `obj` hoặc `.vs` lên Git. Các đường dẫn này đã được khai báo trong `.gitignore`.

## Phạm vi bản demo

- Khách đặt sân không cần tài khoản, để lại tên và thông tin liên hệ.
- Giữ chỗ và VietQR hết hạn sau 10 phút.
- Admin xác nhận cọc thủ công; webhook thật chưa bắt buộc cho bản demo.
- Booking quá giờ thi đấu tự hiển thị là `Đã hoàn thành`.
- Admin có tab booking đang xử lý, lịch sử và công cụ dọn dữ liệu cũ an toàn.
- Chủ kèo duyệt người tham gia; liên hệ điện thoại/Zalo được mở sau khi xác nhận cọc.

Xem [DEMO-CHECKLIST.md](DEMO-CHECKLIST.md) để kiểm tra các luồng trước khi trình diễn.
