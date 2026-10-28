# RenderBatch — bản làm lại bằng ảnh mẫu

Dự án mới, không tiếp tục mã OCR cũ. C#/.NET 10 + WPF + OpenCvSharp, tham khảo cơ chế template matching đã xác minh trong RenderCapcutGUI.exe. Báo cáo: [docs/REFERENCE-ANALYSIS.md](docs/REFERENCE-ANALYSIS.md).

**Trạng thái:** lõi, giao diện học ảnh và kiểm thử đã triển khai. Cần học ảnh trên PC chạy CapCut và thử một project ngắn; chưa xác minh end-to-end trên Windows có CapCut. Bản mới không đóng gói các ảnh tiếng Anh của app tham khảo làm cấu hình sẵn cho giao diện tiếng Việt.

## Chạy

Giải nén toàn bộ gói Windows x64, mở `RenderBatch.Windows.exe`. Gói self-contained không cần Python hoặc .NET SDK. OpenCV sử dụng Microsoft Visual C++ x64 runtime; nếu máy chưa có, cài bản chính thức tại https://aka.ms/vs/17/release/vc_redist.x64.exe .

Từ source: cài .NET 10 SDK và chạy `start.bat`. Cần CapCut 9.6.0, desktop mở khóa, cùng mức quyền với ứng dụng.

## Lần đầu

1. Chọn CapCut.exe, thư mục đầu ra, độ phân giải (720p/1080p/4k), FPS (30/60), thời gian tối đa mỗi video.
2. Mở CapCut ở **chế độ danh sách**. Học các ảnh cần dùng theo [hướng dẫn](docs/TEACHING.md). Một ảnh dùng để nhận diện có thể là nhãn cố định, còn điểm bấm nằm ở ô nhập/hàng bên cạnh. Mỗi ảnh/điểm bấm do bạn ghi nhận từ màn hình thực tế, không dùng tọa độ toàn màn hình đoán sẵn.
3. Dùng **Tìm ảnh, chỉ trỏ chuột** để kiểm tra từng ảnh/điểm bấm, đặc biệt nút tìm kiếm và hàng kết quả đầu tiên. Không bấm trong chế độ này.
4. Nhập file `.txt` UTF-8, mỗi dòng một tên chính xác, theo thứ tự xuất. Tên trùng/không dùng được làm tên file Windows bị từ chối.
5. Thử một project ngắn trước. Khi đủ ảnh đã học, bấm **Chạy / Tiếp tục**. Mỗi file là `<tên project>.mp4`; file có sẵn không bị ghi đè.

Canvas của project giữ nguyên, vì vậy chuẩn bị project 16:9 từ trước. Codec/bitrate giữ thiết lập CapCut hiện tại. FPS mặc định 30. Không có chức năng đăng/chia sẻ video.

## Điều khiển và kiểm tra

Luồng: ảnh trang chủ → mở tìm kiếm nếu cần → nhập/copy lại tên để kiểm tra → bấm hàng đầu tiên sau lọc → mở hộp xuất → so sánh tiêu đề Xuất/Export với tên yêu cầu → đặt tên và thư mục → đọc lại toàn bộ đường dẫn → chọn/kiểm tra độ phân giải, FPS, MP4 → xuất → ảnh thành công **và** file không rỗng, mở độc quyền được → đóng thông báo → đóng editor bình thường và chờ trang chủ.

Các ảnh tìm trong vùng đã học của cửa sổ CapCut, không trên toàn desktop. Đổi kích thước cửa sổ hoặc DPI có thể cần học lại. Bản đầu yêu cầu kích thước/DPI đã học ở từng màn hình; cửa sổ thành công cho phép dao động chiều cao trong vùng giới hạn. Không đọc đúng ảnh, khớp nhiều nơi hoặc không xác minh giá trị thì dừng.

Tìm kiếm của CapCut có thể trả về nhiều tên gần giống. Bản này mở kết quả đầu tiên như app tham khảo nhưng **không xuất** nếu tiêu đề không khớp chính xác. Trường hợp này cần người dùng xử lý truy vấn/tên gần giống; chưa tự duyệt các hàng tiếp theo.

Mặc định đặt thư mục bằng hộp chọn thư mục chuẩn Windows. Hộp chọn tùy biến Qt chưa được hỗ trợ. Có thể bỏ chọn chế độ hộp Windows nếu ô đường dẫn full path cho phép nhập trực tiếp. Cả hai cách đều phải đọc lại đúng full path trước khi xuất; ô chỉ đọc không cho copy hoặc hộp chọn khác sẽ dừng.

## Dừng / tiếp tục

Nút Dừng hoặc đưa chuột về góc trên trái hủy các thao tác tiếp theo, không hủy render đang chạy trong CapCut. Không dùng chuột/bàn phím, khóa desktop hoặc mở một phiên AutoRender/CapCut khác trong lúc chạy.

Cấu hình, ảnh đã học, trạng thái, nhật ký nằm ở `%LOCALAPPDATA%\RenderBatchTemplates`. Job bị gián đoạn được đánh dấu Cần kiểm tra. Khi nhập lại/reset trạng thái, bạn phải kiểm tra CapCut đã dừng công việc cũ; video có sẵn luôn được giữ. Job Hoàn tất được bỏ qua khi tiếp tục cùng cấu hình/danh sách. Nếu về trang chủ thất bại sau khi đã xuất, job vẫn Hoàn tất, các job sau chưa chạy.

Đây là kiểm tra thao tác và file đầu ra, không giải mã toàn bộ video để xác minh nội dung. Hãy kiểm tra file ngắn đầu tiên trước khi chạy các video dài.

## Phát triển

```powershell
dotnet test tests/RenderBatch.Tests/RenderBatch.Tests.csproj -c Release
dotnet build src/RenderBatch.Windows/RenderBatch.Windows.csproj -c Release
dotnet publish src/RenderBatch.Windows/RenderBatch.Windows.csproj -c Release -r win-x64 --self-contained true -o artifacts/windows
```

Chạy test và build tuần tự do dùng chung output core. Linux chạy được test OpenCV với native runtime slim; WPF chỉ chạy trên Windows. Dùng checkout hiện có, không cần Git worktree. Cấu hình repository đã lưu của môi trường không bị thay đổi trong lần làm lại này.
