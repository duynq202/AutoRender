# RenderBatch 1.1.13 — Windows x64

[Tải RenderBatch 1.1.13](https://github.com/duynq202/AutoRender/raw/refs/heads/downloads/renderbatch-templates/downloads/RenderBatch-1.1.13-Windows-x64.zip)

Đóng RenderBatch cũ, giải nén toàn bộ rồi chạy `RenderBatch.Windows.exe`; thanh tiêu đề ghi 1.1.13. Có sẵn .NET runtime, nhận CapCut 9.6.0 / 9.7.0. Dùng lại 7 ảnh đã học.

Giới hạn và giá trị mặc định mới của **Chờ xuất** là **500 phút** cho mỗi project. Có thể chọn từ 1 đến 500 phút. Cấu hình đã lưu được giữ nguyên nếu trong khoảng này; nếu đang là 180, nhập **500** vào ô Chờ xuất trước khi chạy. Giá trị lớn hơn 500 được đưa về 500 khi mở ứng dụng.

Vẫn quét ảnh nút Đóng mỗi 5 giây và xác nhận lại sau 1 giây. Tại phút 500, lần quét cuối vẫn được xác nhận lại thêm 1 giây. Bước mở bảng xuất giữ giới hạn 120 giây. Tùy chọn tắt máy sau toàn bộ hàng đợi và đếm ngược 30 giây vẫn được giữ.

91 kiểm thử đạt, bao gồm 6.000 lần quét đến mốc 500 phút và hoàn tất tại lần quét cuối; build/publish Windows thành công. Chưa kiểm chứng thao tác trực tiếp trên Windows có CapCut.

[Hướng dẫn](../README.md) · [Học ảnh](../docs/TEACHING.md) · [Mã nguồn](https://github.com/duynq202/AutoRender/tree/rebuild/rendercapcut-template). Kiểm tra SHA-256 trong `SHA256SUMS.txt`. Các gói phiên bản trước được giữ ở file riêng.
