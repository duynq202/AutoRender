# RenderBatch 1.1.12 — Windows x64

[Tải RenderBatch 1.1.12](https://github.com/duynq202/AutoRender/raw/refs/heads/downloads/renderbatch-templates/downloads/RenderBatch-1.1.12-Windows-x64.zip)

Đóng RenderBatch cũ, giải nén toàn bộ rồi chạy `RenderBatch.Windows.exe`; thanh tiêu đề ghi 1.1.12. Có sẵn .NET runtime, nhận CapCut 9.6.0 / 9.7.0. Dùng lại nguyên 7 ảnh đã học.

Thêm **Tắt máy khi hoàn tất tất cả project**, mặc định chưa bật ở cấu hình cũ/cài mới, lựa chọn được lưu. Đánh dấu trước khi chạy. Khi toàn bộ danh sách hoàn tất và đóng CapCut thành công, đếm ngược **30 giây**; bấm **Dừng** hoặc đóng RenderBatch để hủy trong thời gian đếm ngược. Project cuối không mở lại CapCut khi chọn tắt máy.

Không gửi lệnh tắt máy khi có lỗi, bấm Dừng, danh sách trống hoặc danh sách đã hoàn tất từ trước. Hủy đếm ngược giữ nguyên trạng thái hoàn tất; bấm Chạy lại với danh sách đó không tự hẹn tắt máy lại. Đổi riêng lựa chọn tắt máy không đặt lại trạng thái hàng đợi. Lệnh tắt máy không ép đóng các ứng dụng khác; Windows có thể yêu cầu xử lý công việc chưa lưu.

Hoàn tất vẫn dựa vào ảnh nút Đóng riêng, quét mỗi 5 giây và xác nhận lại sau 1 giây; MP4 chỉ kiểm tra bổ sung để cảnh báo. Giữ CapCut ở phía trước và desktop mở khóa. Giới hạn quét 180 phút; không ghi log từng lần quét.

91 kiểm thử đạt; build/publish Windows thành công. Kiểm thử dùng lệnh tắt máy giả lập, chưa kiểm chứng thao tác GUI hoặc tắt máy thực tế trên PC Windows có CapCut.

[Hướng dẫn](../README.md) · [Học ảnh](../docs/TEACHING.md) · [Mã nguồn](https://github.com/duynq202/AutoRender/tree/rebuild/rendercapcut-template). Kiểm tra SHA-256 trong `SHA256SUMS.txt`. Các gói phiên bản trước được giữ ở file riêng.
