# RenderBatch 1.1.7 — Windows x64

[Tải RenderBatch 1.1.7](https://github.com/duynq202/AutoRender/raw/refs/heads/downloads/renderbatch-templates/downloads/RenderBatch-1.1.7-Windows-x64.zip)

Giải nén toàn bộ rồi chạy `RenderBatch.Windows.exe`. Có sẵn .NET runtime; không cần Python hoặc .NET SDK. Nhận phiên bản CapCut 9.6.0 / 9.7.0.

Bản này nhận diện hoàn tất bằng bảng có **cặp nút Chia sẻ và Đóng**, không yêu cầu dòng Đã lưu video. Khi nâng từ bản cũ, **bắt buộc học mới một ảnh** trong mục Hoàn tất: cặp nút Chia sẻ và Đóng: kéo vùng chứa cả hai nút trên bảng sau khi xuất xong. Các ảnh khác giữ nguyên; ảnh success cũ vẫn được lưu nhưng không dùng cho hàng đợi mới.

Tìm ảnh hoàn tất trên toàn bộ bảng xuất, cùng DPI lúc học. Cứ mỗi 5 giây, xác nhận cặp nút và file MP4 đúng tên/thư mục có dữ liệu, đã đóng để ghi, rồi mới sang project kế tiếp. Dòng trạng thái phân biệt đang chờ ảnh hay đang chờ file; không nối thêm nhật ký quét. Không nhấn nút Chia sẻ hoặc đăng video.

Giữ cơ chế thử mở bảng xuất mỗi 5 giây trong 120 giây và chờ xuất tối đa 180 phút. 63 kiểm thử đạt; build/publish Windows thành công. Cần học ảnh và thử lại trên PC của bạn, chưa xác minh toàn bộ luồng Windows thực tế.

[Hướng dẫn](../README.md) · [Học ảnh](../docs/TEACHING.md) · [Mã nguồn](https://github.com/duynq202/AutoRender/tree/rebuild/rendercapcut-template). Kiểm tra SHA-256 trong `SHA256SUMS.txt`. Các gói phiên bản trước được giữ ở file riêng.
