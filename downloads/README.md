# RenderBatch 1.1.5 — Windows x64

[Tải RenderBatch 1.1.5](https://github.com/duynq202/AutoRender/raw/refs/heads/downloads/renderbatch-templates/downloads/RenderBatch-1.1.5-Windows-x64.zip)

Giải nén toàn bộ rồi chạy `RenderBatch.Windows.exe`. Có sẵn .NET runtime; không cần Python hoặc .NET SDK.

- Mở project: thử nhận diện/bấm nút Xuất mỗi 5 giây trong tối đa 120 giây. Bảng xuất hiện thì dừng bấm ngay.
- Sau khi bắt đầu xuất: quét thông báo Đã lưu video và kiểm tra file MP4 mỗi **5 giây**, tối đa **180 phút**. Chỉ chuyển project khi xác nhận thành công và file đã ghi xong.
- Không ghi log cho các lần quét định kỳ lên giao diện. Nút Dừng/dừng khẩn cấp vẫn hoạt động. Quá hạn thì dừng hàng đợi và giữ CapCut mở.

Giữ profile 6 ảnh và các video đã xuất. 48 kiểm thử đạt; build/publish Windows thành công. Chưa thử toàn bộ luồng trên PC có CapCut.

[Hướng dẫn](../README.md) · [Học ảnh](../docs/TEACHING.md) · [Mã nguồn](https://github.com/duynq202/AutoRender/tree/rebuild/rendercapcut-template). Kiểm tra SHA-256 trong `SHA256SUMS.txt`. Các gói phiên bản trước được giữ ở file riêng.
