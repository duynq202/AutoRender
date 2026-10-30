# RenderBatch 1.1.3 — Windows x64

[Tải RenderBatch 1.1.3](https://github.com/duynq202/AutoRender/raw/refs/heads/downloads/renderbatch-templates/downloads/RenderBatch-1.1.3-Windows-x64.zip)

Giải nén toàn bộ rồi chạy `RenderBatch.Windows.exe`. Có sẵn .NET runtime; không cần Python hoặc .NET SDK.

Bản sửa bước nhận diện/bấm nút Xuất trong editor:

- Cho phép editor thay đổi kích thước, tìm ảnh nút Xuất theo mép phải/mép trên, cùng DPI lúc học.
- Khi bắt đầu tìm, kích hoạt editor một lần nếu cửa sổ chưa ở phía trước.
- Xác minh hai khung hình ổn định trước khi đưa chuột lên nút, tránh hover làm đổi ảnh giữa hai lần kiểm tra.
- Khi học ảnh, dời chuột khỏi nút trước khi chụp. Nếu ảnh cũ lấy lúc hover, học lại riêng Nút Xuất trong editor.
- Sau 5 giây dò chưa thấy sẽ có lý do; hết hạn lưu thông tin và ảnh chẩn đoán nếu chụp được.

Giữ thời gian chờ mở project **180 giây** theo chỉnh sửa mới trên GitHub (trước đây 60 giây). Chờ xuất tối đa **180 phút**, quét mỗi phút không ghi log định kỳ; giữ profile và các video đã xuất.

40 kiểm thử đạt; build/publish Windows thành công. Cần thử trên PC có CapCut; chưa xác minh nguyên nhân hoặc thao tác thực tế trên máy của bạn.

[Hướng dẫn](../README.md) · [Học ảnh](../docs/TEACHING.md) · [Mã nguồn](https://github.com/duynq202/AutoRender/tree/rebuild/rendercapcut-template). Kiểm tra SHA-256 trong `SHA256SUMS.txt`. Các gói phiên bản trước được giữ ở file riêng.
