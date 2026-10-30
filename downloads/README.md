# RenderBatch 1.1.11 — Windows x64

[Tải RenderBatch 1.1.11](https://github.com/duynq202/AutoRender/raw/refs/heads/downloads/renderbatch-templates/downloads/RenderBatch-1.1.11-Windows-x64.zip)

Đóng RenderBatch cũ, giải nén toàn bộ rồi chạy `RenderBatch.Windows.exe`; thanh tiêu đề ghi 1.1.11. Có sẵn .NET runtime, nhận phiên bản CapCut 9.6.0 / 9.7.0. Giữ nguyên 7 ảnh đã học từ 1.1.8–1.1.10.

Quét ảnh nút Đóng riêng mỗi **5 giây** trên toàn bộ cửa sổ CapCut ở phía trước, không phụ thuộc tên/AutomationId bảng chia sẻ. Khi có ảnh, chụp mới sau **1 giây**; cùng cửa sổ, PID, thời điểm khởi động tiến trình và vị trí ổn định mới xác nhận hoàn tất theo ảnh. Sau đó kết thúc cây tiến trình CapCut đã xác nhận, chờ thoát tối đa 15 giây, mở lại và chờ trang chủ rồi xử lý project tiếp theo. Không chờ bấm nút Đóng của bảng chia sẻ.

**MP4 chỉ dùng kiểm tra bổ sung:** chưa tìm được đúng tên/thư mục, file rỗng hoặc bị khóa thì ghi một cảnh báo, vẫn đóng/mở lại theo ảnh. Trạng thái Hoàn tất theo ảnh không chứng minh file đã được kiểm tra thành công. File đã tồn tại trước khi chạy vẫn bị chặn để tránh ghi đè. Hãy học đúng ảnh nút Đóng ở bảng sau khi xuất xong.

Nhật ký ghi các mốc **Đã thấy nút Đóng → Đang kết thúc CapCut (PID…) → CapCut đã thoát**, cảnh báo và lỗi; không ghi log từng lần quét. Nút Dừng/dừng khẩn cấp vẫn hoạt động. Giới hạn quét 180 phút; lần quét cuối được thêm bước xác nhận 1 giây. Nếu không kết thúc tiến trình được thì dừng, chưa mở phiên mới hoặc chạy project tiếp theo.

Giữ CapCut ở phía trước, desktop mở khóa. Về trang chủ CapCut trước khi bắt đầu hàng đợi; bỏ project đã có MP4 khỏi danh sách để tránh chặn ghi đè. Cặp nút Chia sẻ và Đóng vẫn được giữ trong profile nhưng không thay thế ảnh nút Đóng riêng khi tự động xác nhận.

82 kiểm thử đạt; build/publish Windows thành công. Chưa xác minh thao tác trực tiếp trên PC có CapCut.

[Hướng dẫn](../README.md) · [Học ảnh](../docs/TEACHING.md) · [Mã nguồn](https://github.com/duynq202/AutoRender/tree/rebuild/rendercapcut-template). Kiểm tra SHA-256 trong `SHA256SUMS.txt`. Các gói phiên bản trước được giữ ở file riêng.
