# Adapter OCR thử nghiệm v0.3.2

Bản này dùng bốn ảnh của người dùng làm tham chiếu bố cục. Chưa chạy kiểm thử end-to-end trên Windows có CapCut. Kiểm thử logic và cross-build không chứng minh thao tác thực tế đã hoạt động.

## Thử lần đầu

1. Đóng AutoRender cũ, giải nén ZIP mới vào một thư mục mới, mở `AutoRender.App.exe`. Tiêu đề phải có **OCR thử nghiệm v0.3.2**.
2. Chọn `CapCut.exe`, giữ profile `capcut-9.6.vi.visual.json` (Engine=Visual). Nếu profile cũ còn lưu trong preferences, đường dẫn template mặc định cũ được chuyển sang profile OCR tự động; nếu chọn profile riêng, cần chọn lại profile OCR mới.
3. Mở trang chủ CapCut, nhấn **Kiểm tra OCR**. Chức năng này chụp/đọc chữ, đưa CapCut lên trước và ghi chẩn đoán, không gõ vào ô nhập hoặc bấm nút xuất. Kiểm tra có đọc được `Dự án` và tên project cần dùng.
4. Chuẩn bị file `test.txt` chỉ có tên project ngắn (ví dụ `1027`), chọn thư mục đầu ra mới không có file trùng, nhập danh sách, chọn 720P hoặc 1080P và 30 FPS. Xác nhận canvas đã là 16:9.
5. Chọn hàng project trong AutoRender, nhấn **Xem vị trí project (không bấm)**. Ứng dụng tìm trong khung hiện tại trước rồi mới cuộn nếu cần, đưa con trỏ tới hàng/thumbnail đã tìm, không nhấn chuột vào nội dung. Nếu con trỏ đúng, nhấn **Chạy / Tiếp tục** và quan sát lần đầu. Nếu lỗi, không thử cả danh sách dài: kiểm tra `%LOCALAPPDATA%\AutoRenderNative\diagnostics` và `activity.log`. Gửi ảnh PNG và TXT của bước lỗi cho người phát triển. Ảnh được phóng 2 lần để OCR; các tọa độ nhật ký tính theo ảnh gốc trong cửa sổ.

## Cách điều khiển

- Dùng `AutomationId` cửa sổ đã thấy trong khảo sát: HomeWindow, MainWindow, ExportWindow_Container.
- Trang chủ: đọc khung nhìn hiện tại trước khi cuộn, để không bỏ qua project đang hiển thị. Nếu chưa xác định được, di chuyển chuột tới vùng cuộn và gửi bánh xe, không bấm nội dung để lấy focus. Trong chế độ danh sách, phải thấy tên project và cột dung lượng/thời lượng cùng một hàng; double-click khoảng trống cột Tên ở đúng cao độ hàng. Trong chế độ lưới, dùng tiêu đề Dự án hoặc metadata ngay dưới tên để xác nhận card rồi bấm thumbnail phía trên tên. Có xử lý cách OCR đọc tiêu đề `Dự án` thành `DIJ ån` như log thực tế. Trước khi mở, chờ 1,2 giây và đọc lại vị trí; nếu đổi bố cục thì dừng. Tối đa 50 lần quét hoặc thời gian chờ mở project.
- Hộp xuất: OCR tìm nhãn Tên/Xuất sang/Độ phân giải/Tỷ lệ khung hình/Định dạng. Giá trị và ô nhập nằm ở cột bên phải theo ảnh. Đường dẫn Xuất sang gồm **thư mục + tên + .mp4**, không chỉ thư mục. Đọc lại đầy đủ ô Tên và Xuất sang bằng clipboard; không ghi nội dung clipboard không khớp vào log.
- Độ phân giải, FPS và MP4 được nhận diện lại sau khi chọn. Codec và bitrate giữ thiết lập CapCut hiện tại.
- Có thông báo Đã lưu video và nút Đóng mới được coi là trạng thái thành công. Ảnh tham chiếu còn Đang tạo...66% cho bản xem trước 9:16; adapter chờ khi vẫn nhận được tiến trình dưới 100% hoặc trạng thái đang tạo chưa đọc được phần trăm. File đầu ra còn phải có dữ liệu và không bị tiến trình khác giữ mở.
- Sau khi xong, bấm Đóng rồi dùng WindowPattern.Close cho editor và chờ HomeWindow. Không dùng nút Chia sẻ, không thay canvas hoặc tạo nội dung mới.

## Yêu cầu và giới hạn

Windows OCR phải có ít nhất một ngôn ngữ nhận diện được cài. Không cần Python hoặc API/cloud OCR. Việc nhận diện chạy cục bộ.

Giữ desktop mở khóa, cửa sổ trên màn hình và không dùng chuột/bàn phím khi chạy. Ứng dụng kiểm tra focus còn thuộc CapCut trước thao tác; khi focus đổi sẽ dừng. Dừng trong AutoRender hoặc đưa chuột về góc trên trái ngừng thao tác tiếp theo, không hủy render trong CapCut. Clipboard bị thay đổi vì dùng để đọc lại ô nhập.

Bản thử chưa xử lý mọi kiểu popup, hộp chọn thư mục, tên bị cắt, màn hình HDR hoặc menu nằm ngoài vùng ảnh. Không tự đoán khi nhận diện thất bại. Nếu ô Xuất sang là chỉ đọc hoặc mở hộp chọn khác, cần bổ sung xử lý hộp chọn sau khi khảo sát. Lỗi này sẽ dừng trước khi nhấn Xuất.

## Sửa lỗi bấm nhầm thẻ AI (0.3.1)

Bản 0.3.0 đã bấm vào giữa trang để lấy focus trước khi cuộn, vị trí này trùng thẻ AI trên máy người dùng. Bản 0.3.1 loại bỏ thao tác bấm đó và bỏ tìm kiếm theo tọa độ ước lượng. Dùng nút xem vị trí để kiểm tra lần đầu; nếu không thấy đúng tên, công cụ dừng và lưu chẩn đoán thay vì bấm một project khác. Bản 0.3.1 chỉ hỗ trợ lưới; bản 0.3.2 đã bổ sung danh sách như phần dưới.

## Sửa nhận diện danh sách và thông báo không tìm thấy (0.3.2)

Log thực tế có tên `1027` tại `[308,812]`, dung lượng `114.1K` tại `[1048,812]`, thời lượng `00:03` tại `[1150,812]`. Đây là danh sách theo hàng. Tiêu đề Dự án bị OCR thành `DIJ ån`; bản cũ yêu cầu chữ Dự án phải khớp và chỉ dùng offset thumbnail của lưới, nên loại tên dù OCR đọc đúng. Bản 0.3.2 nhận diện hàng bằng các cột cùng cao độ; không lấy điểm phía trên tên để bấm trong danh sách. Điểm `[308,648]` trong báo cáo trước cũng được kiểm thử với metadata cùng hàng.

Không có đủ bằng chứng thì không tự bấm. Khi tên đã được OCR đọc, nhật ký ghi tọa độ và lý do loại; ảnh/TXT `project-rejected-*` lưu ở thư mục diagnostics. Đây là kiểm thử thuật toán trên dữ liệu log, chưa phải xác minh export end-to-end trên PC đích.
