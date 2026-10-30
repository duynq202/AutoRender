# RenderBatch 1.1.9 — Windows x64

[Tải RenderBatch 1.1.9](https://github.com/duynq202/AutoRender/raw/refs/heads/downloads/renderbatch-templates/downloads/RenderBatch-1.1.9-Windows-x64.zip)

Đóng RenderBatch cũ, giải nén toàn bộ rồi chạy `RenderBatch.Windows.exe`; thanh tiêu đề ghi 1.1.9. Có sẵn .NET runtime, nhận phiên bản CapCut 9.6.0 / 9.7.0.

Bước chờ xuất/quét ảnh nút Đóng tiếp tục mỗi **5 giây**. Khi đã nhận diện video hoàn tất và file MP4 ghi xong, nếu chưa tìm hoặc chưa bấm được Đóng, quay lại quét sau 5 giây. Không vào vòng chờ riêng 180 giây của thao tác click. Chỉ đóng/mở lại CapCut và sang project tiếp theo khi đã xử lý xong bảng hoàn tất.

Giới hạn chung vẫn 180 phút từ lúc bắt đầu render; nút Dừng/dừng khẩn cấp vẫn hoạt động. Không ghi log từng lần quét. Video đã ghi xong giữ trạng thái Hoàn tất nếu bước đóng bị gián đoạn.

Dùng lại nguyên 7 ảnh đã học ở 1.1.8, không cần học mới. Nếu nâng từ 1.1.7 còn thiếu ảnh nút Đóng riêng, học mục Hoàn tất: nút Đóng riêng trước. Về trang chủ CapCut trước khi chạy hàng đợi.

70 kiểm thử đạt; build/publish Windows thành công. Chưa xác minh thao tác trực tiếp trên PC của bạn.

[Hướng dẫn](../README.md) · [Học ảnh](../docs/TEACHING.md) · [Mã nguồn](https://github.com/duynq202/AutoRender/tree/rebuild/rendercapcut-template). Kiểm tra SHA-256 trong `SHA256SUMS.txt`. Các gói phiên bản trước được giữ ở file riêng.
