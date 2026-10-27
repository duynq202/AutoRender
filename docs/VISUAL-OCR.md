# Adapter OCR thử nghiệm v0.3

Bản này dùng bốn ảnh của người dùng làm tham chiếu bố cục. Chưa chạy kiểm thử end-to-end trên Windows có CapCut. Kiểm thử logic và cross-build không chứng minh thao tác thực tế đã hoạt động.

## Thử lần đầu

1. Đóng AutoRender cũ, giải nén ZIP mới vào một thư mục mới, mở `AutoRender.App.exe`. Tiêu đề phải có **OCR thử nghiệm v0.3**.
2. Chọn `CapCut.exe`, giữ profile `capcut-9.6.vi.visual.json` (Engine=Visual). Nếu profile cũ còn lưu trong preferences, đường dẫn template mặc định cũ được chuyển sang profile OCR tự động; nếu chọn profile riêng, cần chọn lại profile OCR mới.
3. Mở trang chủ CapCut, nhấn **Kiểm tra OCR**. Chức năng này chụp/đọc chữ, đưa CapCut lên trước và ghi chẩn đoán, không gõ vào ô nhập hoặc bấm nút xuất. Kiểm tra có đọc được `Dự án` và tên project cần dùng.
4. Chuẩn bị file `test.txt` chỉ có tên project ngắn (ví dụ `1027`), chọn thư mục đầu ra mới không có file trùng, nhập danh sách, chọn 720P hoặc 1080P và 30 FPS. Xác nhận canvas đã là 16:9.
5. Chạy và quan sát lần đầu. Nếu lỗi, không thử cả danh sách dài: kiểm tra `%LOCALAPPDATA%\AutoRenderNative\diagnostics` và `activity.log`. Gửi ảnh PNG và TXT của bước lỗi cho người phát triển. Ảnh được phóng 2 lần để OCR; các tọa độ nhật ký tính theo ảnh gốc trong cửa sổ.

## Cách điều khiển

- Dùng `AutomationId` cửa sổ đã thấy trong khảo sát: HomeWindow, MainWindow, ExportWindow_Container.
- Trang chủ: OCR xác định tiêu đề Dự án. Icon tìm kiếm được lấy theo bố cục ảnh: 336 pixel logic từ mép phải cửa sổ, thẳng hàng tiêu đề, điều chỉnh theo DPI. Gõ tên, đọc đúng tên card rồi double-click thumbnail phía trên; sau đó đọc lại tên ở thanh tiêu đề editor. Nếu bố cục khác hoặc tên bị rút gọn, dừng để hiệu chỉnh.
- Hộp xuất: OCR tìm nhãn Tên/Xuất sang/Độ phân giải/Tỷ lệ khung hình/Định dạng. Giá trị và ô nhập nằm ở cột bên phải theo ảnh. Đường dẫn Xuất sang gồm **thư mục + tên + .mp4**, không chỉ thư mục. Đọc lại đầy đủ ô Tên và Xuất sang bằng clipboard; không ghi nội dung clipboard không khớp vào log.
- Độ phân giải, FPS và MP4 được nhận diện lại sau khi chọn. Codec và bitrate giữ thiết lập CapCut hiện tại.
- Có thông báo Đã lưu video và nút Đóng mới được coi là trạng thái thành công. Ảnh tham chiếu còn Đang tạo...66% cho bản xem trước 9:16; adapter chờ khi vẫn nhận được tiến trình dưới 100% hoặc trạng thái đang tạo chưa đọc được phần trăm. File đầu ra còn phải có dữ liệu và không bị tiến trình khác giữ mở.
- Sau khi xong, bấm Đóng rồi dùng WindowPattern.Close cho editor và chờ HomeWindow. Không dùng nút Chia sẻ, không thay canvas hoặc tạo nội dung mới.

## Yêu cầu và giới hạn

Windows OCR phải có ít nhất một ngôn ngữ nhận diện được cài. Không cần Python hoặc API/cloud OCR. Việc nhận diện chạy cục bộ.

Giữ desktop mở khóa, cửa sổ trên màn hình và không dùng chuột/bàn phím khi chạy. Ứng dụng kiểm tra focus còn thuộc CapCut trước thao tác; khi focus đổi sẽ dừng. Dừng trong AutoRender hoặc đưa chuột về góc trên trái ngừng thao tác tiếp theo, không hủy render trong CapCut. Clipboard bị thay đổi vì dùng để đọc lại ô nhập.

Bản thử chưa xử lý mọi kiểu popup, hộp chọn thư mục, tên bị cắt, màn hình HDR hoặc menu nằm ngoài vùng ảnh. Không tự đoán khi nhận diện thất bại. Nếu ô Xuất sang là chỉ đọc hoặc mở hộp chọn khác, cần bổ sung xử lý hộp chọn sau khi khảo sát. Lỗi này sẽ dừng trước khi nhấn Xuất.
