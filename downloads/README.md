# RenderBatch 1.1.14 — Windows x64

[Tải RenderBatch 1.1.14](https://github.com/duynq202/AutoRender/raw/refs/heads/downloads/renderbatch-templates/downloads/RenderBatch-1.1.14-Windows-x64.zip)

Đóng RenderBatch cũ, giải nén toàn bộ rồi chạy `RenderBatch.Windows.exe`; thanh tiêu đề ghi 1.1.14. Có sẵn .NET runtime, nhận CapCut 9.6.0 / 9.7.0. Dùng lại 7 ảnh đã học.

Sau khi mở project, tìm và thử bấm nút Xuất trong editor mỗi **5 giây**, tối đa **5 phút (300 giây)**. Chỉ bấm khi ảnh/vị trí đã xác nhận ổn định. Dừng thử ngay khi bảng xuất xuất hiện, kể cả khi nội dung còn tải. Không chờ cố định đủ 5 phút nếu bảng đã sẵn sàng.

Nếu hết 5 phút vẫn chưa mở được bảng xuất, dừng hàng đợi và giữ CapCut mở; không chạy project tiếp theo hoặc tự tắt máy. Nút Dừng và dừng khẩn cấp vẫn hoạt động giữa các lần thử.

Giới hạn chờ render vẫn **500 phút**; quét ảnh nút Đóng mỗi 5 giây, xác nhận lại sau 1 giây. Tùy chọn tắt máy khi hoàn tất toàn bộ danh sách và đếm ngược 30 giây vẫn được giữ. Nếu cấu hình cũ đang lưu 180 phút, đổi ô Chờ xuất thành 500 trước khi chạy khi cần.

91 kiểm thử đạt, bao gồm bảng xuất xuất hiện ở giây 242 và hết hạn ở giây 300 sau 60 lần thử; build/publish Windows thành công. Chưa kiểm chứng thao tác trực tiếp trên Windows có CapCut.

[Hướng dẫn](../README.md) · [Học ảnh](../docs/TEACHING.md) · [Mã nguồn](https://github.com/duynq202/AutoRender/tree/rebuild/rendercapcut-template). Kiểm tra SHA-256 trong `SHA256SUMS.txt`. Các gói phiên bản trước được giữ ở file riêng.
