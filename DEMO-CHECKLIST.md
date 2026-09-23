# SportMatch — kiểm tra trước khi demo

## Chuẩn bị

- Chọn `SportMatch.Web` làm Startup Project rồi bấm F5.
- Đăng nhập admin và kiểm tra dashboard mở bình thường.
- Không cần tên miền hoặc SePay thật; dùng nút xác nhận cọc thủ công trong admin.

## Đặt sân và tự mở kèo

1. Chọn sân và khung giờ còn trống.
2. Bật **Mở kèo tìm thêm người**, nhập số người, trình độ và chi phí mỗi người.
3. Xác nhận booking và mở trang QR.
4. Vào admin, chọn **Xác nhận đã nhận cọc**.
5. Quay lại **Lịch của tôi**; booking phải có liên kết **Quản lý kèo đã mở**.
6. Kèo mới phải xuất hiện trên trang **Tìm kèo**.

## Hủy và hoàn cọc

- Booking còn trên 24 giờ: dự kiến hoàn 100%.
- Booking còn từ 12 đến 24 giờ: dự kiến hoàn 50%.
- Booking còn dưới 12 giờ: không được gửi yêu cầu hủy.
- Khi admin duyệt hủy booking có kèo, kèo liên quan phải chuyển sang **Đã đóng**.

## Quản trị sân

- Thay ảnh riêng của từng sân con.
- Xóa ảnh riêng: sân con quay về dùng ảnh cụm sân.
- Ẩn sân không có booking sắp tới: sân biến mất khỏi trang khách.
- Ẩn sân còn booking sắp tới: hệ thống phải chặn và hiện cảnh báo.
- Bật lại sân: sân xuất hiện trở lại.

## Cáp kèo

- Chủ kèo duyệt yêu cầu tham gia.
- Người tham gia nhận QR cọc 50%.
- Admin xác nhận cọc demo và thông tin liên hệ/Zalo được mở.
- Không thể nhận quá số vị trí cần tuyển.

## Kiểm tra giao diện

- Chrome hoặc Edge trên máy tính.
- Chế độ responsive kích thước điện thoại.
- Nút Zalo mở đúng số `0886415153`.
- Không có lỗi đỏ trong Visual Studio khi thực hiện các luồng trên.

## Hoàn thành và lịch sử booking

- Booking đã xác nhận và qua hết giờ sân phải hiển thị `Đã hoàn thành`.
- Trang admin mặc định chỉ hiện booking đang xử lý; booking cũ nằm trong tab **Lịch sử**.
- Công cụ dọn lịch sử không được xóa booking tương lai hoặc booking còn chờ hoàn tiền.
