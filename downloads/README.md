# RenderBatch 1.1.10 — Windows x64

[Tải RenderBatch 1.1.10](https://github.com/duynq202/AutoRender/raw/refs/heads/downloads/renderbatch-templates/downloads/RenderBatch-1.1.10-Windows-x64.zip)

Đóng RenderBatch cũ, giải nén toàn bộ rồi chạy `RenderBatch.Windows.exe`; thanh tiêu đề ghi 1.1.10. Có sẵn .NET runtime, nhận phiên bản CapCut 9.6.0 / 9.7.0.

Quét ảnh nút Đóng hoặc cặp nút Chia sẻ và Đóng mỗi **5 giây**, tối đa **180 phút**. Khi ảnh hoàn tất khớp và MP4 đúng tên/thư mục đã ghi xong, đóng **ứng dụng CapCut trực tiếp** rồi mở lại, chờ trang chủ và xử lý project tiếp theo. Không chờ bấm nút Đóng của bảng chia sẻ.

Gửi yêu cầu đóng bình thường trước; nếu phiên vẫn chạy sau 10 giây, kết thúc cây tiến trình của phiên CapCut đã xác minh. Chờ thoát tối đa 15 giây nữa trước khi mở lại. Nếu không thoát được thì dừng, không chạy project tiếp theo. Chỉ kết thúc tiến trình sau khi đã xác nhận xuất xong; nút Dừng/dừng khẩn cấp vẫn hoạt động. Không ghi log từng lần quét.

Dùng lại nguyên 7 ảnh đã học ở 1.1.8 / 1.1.9. Trong RenderBatch chọn đúng thư mục đang lưu video của CapCut; file cần mang tên `<tên project>.mp4`. Nếu file chưa có hoặc còn bị khóa, tiếp tục chờ và hiện đường dẫn trong dòng trạng thái. Về trang chủ CapCut trước khi chạy hàng đợi; bỏ project đã có MP4 khỏi danh sách để tránh chặn ghi đè.

76 kiểm thử đạt; build/publish Windows thành công. Chưa xác minh thao tác trực tiếp trên PC của bạn.

[Hướng dẫn](../README.md) · [Học ảnh](../docs/TEACHING.md) · [Mã nguồn](https://github.com/duynq202/AutoRender/tree/rebuild/rendercapcut-template). Kiểm tra SHA-256 trong `SHA256SUMS.txt`. Các gói phiên bản trước được giữ ở file riêng.
