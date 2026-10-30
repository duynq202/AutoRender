# RenderBatch 1.1.4 — Windows x64

[Tải RenderBatch 1.1.4](https://github.com/duynq202/AutoRender/raw/refs/heads/downloads/renderbatch-templates/downloads/RenderBatch-1.1.4-Windows-x64.zip)

Giải nén toàn bộ rồi chạy `RenderBatch.Windows.exe`. Có sẵn .NET runtime; không cần Python hoặc .NET SDK.

Sau khi mở project, thử nhận diện/bấm nút Xuất ngay, rồi mỗi 5 giây một lần trong tối đa 120 giây. Giữa các lần thử kiểm tra bảng xuất khoảng mỗi 0,5 giây; bảng xuất hiện thì dừng bấm ngay. Mỗi lần bấm kiểm tra lại ảnh, vị trí và trạng thái bảng xuất, tránh bấm tiếp vào bảng đã mở. Không còn khoảng chờ cố định 180 giây.

Nếu quá 120 giây chưa có bảng xuất thì dừng hàng đợi, giữ CapCut mở và lưu chẩn đoán. Nút Dừng và góc dừng khẩn cấp vẫn hoạt động. Sau khi bắt đầu render, giữ giới hạn chờ 180 phút và quét thông báo đã lưu mỗi phút, không ghi log từng lần quét.

Giữ profile 6 ảnh hiện có, cấu hình và video đã xuất. Cần học ảnh trên đúng PC/DPI CapCut của bạn.

48 kiểm thử đạt; build/publish Windows thành công. Chưa thử toàn bộ luồng trên PC có CapCut.

[Hướng dẫn](../README.md) · [Học ảnh](../docs/TEACHING.md) · [Mã nguồn](https://github.com/duynq202/AutoRender/tree/rebuild/rendercapcut-template). Kiểm tra SHA-256 trong `SHA256SUMS.txt`. Các gói phiên bản trước được giữ ở file riêng.
