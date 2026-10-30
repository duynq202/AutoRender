# RenderBatch 1.1.2 — Windows x64

[Tải RenderBatch 1.1.2](https://github.com/duynq202/AutoRender/raw/refs/heads/downloads/renderbatch-templates/downloads/RenderBatch-1.1.2-Windows-x64.zip)

Giải nén toàn bộ rồi chạy `RenderBatch.Windows.exe`. Có sẵn .NET runtime; không cần Python hoặc .NET SDK.

- Bấm icon tìm kiếm rồi nhập tên trực tiếp; vẫn chỉ cần 6 ảnh đã học.
- Mở project xong chờ 60 giây, sau đó tiếp tục dò nút Xuất tối đa 180 giây mặc định.
- Chờ xuất tối đa 180 phút; mỗi phút quét thông báo đã lưu và kiểm tra file MP4. Không ghi log cho các lần quét định kỳ lên giao diện.
- Xác nhận thành công rồi mới đóng/mở lại CapCut. Hết hạn chưa có kết quả thì dừng hàng đợi và giữ CapCut mở. Nút Dừng/dừng khẩn cấp vẫn hoạt động giữa các lần quét.

Cấu hình cũ 720 phút tự hạ xuống 180 phút; giữ profile và trạng thái project. Đặt sẵn thư mục, MP4, độ phân giải/FPS trong CapCut; chọn cùng thư mục trong RenderBatch để xác nhận video.

35 kiểm thử đạt; build/publish Windows thành công. Cần thử trên PC có CapCut; chưa kiểm chứng toàn bộ luồng Windows thực tế.

[Hướng dẫn](../README.md) · [Học ảnh](../docs/TEACHING.md) · [Mã nguồn](https://github.com/duynq202/AutoRender/tree/rebuild/rendercapcut-template). Kiểm tra SHA-256 trong `SHA256SUMS.txt`. Các gói phiên bản trước được giữ ở file riêng.
