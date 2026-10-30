# RenderBatch 1.1.1 — Windows x64

[Tải RenderBatch 1.1.1](https://github.com/duynq202/AutoRender/raw/refs/heads/downloads/renderbatch-templates/downloads/RenderBatch-1.1.1-Windows-x64.zip)

Giải nén toàn bộ rồi chạy `RenderBatch.Windows.exe`. Có sẵn .NET runtime; không cần Python hoặc .NET SDK.

Bản này bấm icon tìm kiếm rồi nhập tên trực tiếp, không cần tìm/học ảnh ô nhập. Chỉ cần 6 ảnh: trang chủ, icon tìm kiếm, hàng project, Xuất trong editor, bắt đầu xuất, đã lưu video. Để ô tìm kiếm đóng trước khi bắt đầu hàng đợi.

Sau khi mở project, chờ đủ 30 giây rồi tiếp tục dò nút Xuất (mặc định tối đa 180 giây nữa). Có thể dừng/hủy khi đang chờ. Nếu hết thời gian, giữ CapCut mở và không chạy project tiếp theo. Xuất xong và xác nhận file MP4 đã ghi xong mới đóng/mở lại CapCut.

Đặt sẵn thư mục, MP4, độ phân giải/FPS trong CapCut; chọn cùng thư mục trong RenderBatch để xác nhận video. Profile cũ được giữ; ảnh ô nhập tìm kiếm không còn bắt buộc.

29 kiểm thử lõi đạt; build WPF/publish Windows thành công. Cần thử project ngắn trên PC của bạn; chưa kiểm chứng toàn bộ luồng CapCut trên Windows.

[Hướng dẫn học ảnh](../docs/TEACHING.md) · [Mã nguồn](https://github.com/duynq202/AutoRender/tree/rebuild/rendercapcut-template). Kiểm tra SHA-256 trong `SHA256SUMS.txt`. Các gói phiên bản trước được giữ ở file riêng.
