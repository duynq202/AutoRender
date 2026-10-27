# AutoRender — CapCut 9.6.0 tiếng Việt

Ứng dụng Windows WPF/.NET 10 để xuất tuần tự project CapCut đã có sẵn theo danh sách tên. Thiết kế mới hoàn toàn bằng C#, không dùng Python hoặc giao diện cũ.

**Bản 0.3.2 OCR thử nghiệm:** hàng đợi, WPF và bộ điều khiển OCR đã triển khai, build và kiểm thử logic. Chưa chạy xuất thực tế trên PC có CapCut. Profile mặc định `capcut-9.6.vi.visual.json` dùng OCR và vị trí tương đối dựa trên bốn ảnh CapCut 9.6.0 tiếng Việt do người dùng cung cấp. Cần kiểm tra OCR và xuất thử một project ngắn trước khi chạy hàng loạt. Profile UI Automation mẫu cũ vẫn chưa đầy đủ; không bật cờ Verified để bỏ qua kiểm chứng.

## Chạy trên Windows

Bản đóng gói: giải nén toàn bộ `AutoRender-win-x64.zip` rồi mở `AutoRender.App.exe`. Giữ các thư viện và thư mục `profiles` cạnh EXE. Bản này dành cho Windows x64 và kèm .NET runtime.

Từ mã nguồn: cài .NET 10 SDK, chạy `start.bat`. Cần Windows 10 phiên bản 2004 trở lên hoặc Windows 11 x64, CapCut 9.6.0 tiếng Việt đã cài, cùng mức quyền với AutoRender, desktop mở khóa.

Bản OCR dùng Windows OCR có sẵn trên hệ điều hành; không cài Python/Tesseract/OpenCV. Nếu máy chưa có bộ nhận diện, vào Windows Settings → Language → Language options và cài tính năng OCR tiếng Anh (hoặc tiếng Việt nếu Windows cung cấp). OCR chọn tiếng Việt khi có, nếu không dùng tiếng Anh và chuẩn hóa dấu khi đối chiếu.

## Quy trình

1. Giải nén vào thư mục mới, kiểm tra tiêu đề **OCR thử nghiệm v0.3.2**. Giữ profile mặc định `capcut-9.6.vi.visual.json`. Mở CapCut rồi nhấn **Kiểm tra OCR**; xem các dòng chữ trong nhật ký. Ảnh và văn bản chẩn đoán được lưu ở `%LOCALAPPDATA%\AutoRenderNative\diagnostics`. Chi tiết trong [docs/VISUAL-OCR.md](docs/VISUAL-OCR.md).
2. Chọn đường dẫn `CapCut.exe`, dùng profile `Engine=Visual` đi kèm. Trở về trang chủ; bản 0.3.2 nhận diện cả chế độ lưới và danh sách. Ứng dụng chỉ di chuyển chuột/cuộn khi tìm project, không bấm vào nội dung trang để lấy focus hoặc bấm nút tìm kiếm theo vị trí ước lượng.
3. Chọn thư mục đầu ra, độ phân giải (mặc định 1080P), FPS (mặc định 30) và thời gian chờ tối đa mỗi video (mặc định 720 phút). Chỉ chọn thông số có thật trong CapCut trên máy.
4. Chuẩn bị file `.txt` UTF-8: mỗi dòng là **tên chính xác, duy nhất** của một project có sẵn. Ví dụ trong `examples/projects.txt`. Bỏ dòng trống; không tự sắp xếp hoặc bỏ tên trùng. Tên trùng bị từ chối. Tên không hợp lệ làm tên file Windows cũng bị từ chối thay vì tự đổi tên.
5. Chọn **Nhập danh sách .txt**. Thứ tự trong file là thứ tự xuất. Cấu hình xuất được lưu cùng hàng đợi. Muốn thay cấu hình, nhập lại danh sách và xác nhận thay hàng đợi.
6. Xác nhận project đã có canvas **16:9**. Ứng dụng giữ bố cục sẵn có, không cắt/căn lại nội dung và không tự đổi canvas.
7. Giữ CapCut ở trang chủ. Chọn một hàng trong danh sách, nhấn **Xem vị trí project (không bấm)** để kiểm tra con trỏ nằm đúng hàng (danh sách) hoặc thumbnail (lưới). Chức năng này không mở project hoặc xuất video. Sau đó nhấn **Chạy / Tiếp tục**. Driver OCR tìm đúng project theo tên, xác nhận lại tên trên thanh tiêu đề editor, mở hộp xuất, đặt tên và đường dẫn **đầy đủ tới file .mp4**, độ phân giải, FPS và MP4. Ô nhập được đọc lại bằng Ctrl+A/Ctrl+C để xác nhận đầy đủ; các menu được xác nhận lại bằng OCR trước khi xuất. Clipboard sẽ được dùng và thay đổi trong quá trình này.
8. Mỗi video có tên `<tên project>.mp4` trong thư mục đầu ra. Chỉ chuyển tiếp khi OCR thấy thông báo đã lưu và nút Đóng, không còn tiến trình đang tạo dưới 100%, **và** file đầu ra có dữ liệu, có thể mở độc quyền để đọc. Đây chưa phải kiểm tra giải mã toàn bộ nội dung video; hãy thử với video ngắn và kiểm tra kết quả thủ công trước.

## Dừng và khôi phục

- Chỉ một hàng đợi trong một phiên AutoRender được chạy. Khóa phiên ngăn mở hai cửa sổ AutoRender cùng lúc trong cùng phiên đăng nhập Windows.
- **Dừng** hoặc đưa chuột về góc trên trái sẽ ngừng thao tác tiếp theo. Không hủy render đang chạy bên trong CapCut.
- UI Automation gọi sang tiến trình CapCut; nếu provider của CapCut bị treo trong một lệnh gọi, yêu cầu dừng có thể bị trì hoãn. Bản đầu chưa có tiến trình worker riêng để xử lý trường hợp này.
- Khi có lỗi, không tìm thấy project/nút, selector không duy nhất hoặc timeout: dừng hàng đợi, không tự bỏ qua project lỗi.
- SQLite và nhật ký ở `%LOCALAPPDATA%\AutoRenderNative`. Khởi động lại sẽ chuyển công việc bị gián đoạn sang **Cần kiểm tra**; không tự xuất lại.
- Kiểm tra trạng thái CapCut và video trước khi chọn **Cho phép chạy lại hàng đã chọn**. Nếu file đầu ra đã tồn tại, ứng dụng từ chối ghi đè; bạn tự kiểm tra và di chuyển file trước nếu cần xuất lại.
- Công việc đã hoàn tất được giữ nguyên khi tiếp tục. Nếu xuất xong nhưng chưa về được trang chủ, file vẫn ghi nhận hoàn tất; đưa CapCut về trang chủ rồi tiếp tục các hàng còn chờ.
- Không điều khiển chuột/bàn phím, sửa project, thay giao diện hoặc khởi động một tác vụ xuất khác trong lúc hàng đợi hoạt động.

## Giới hạn cần kiểm chứng

Khảo sát trên máy đích đã xác nhận có các cửa sổ `HomeWindow`, `MainWindow`, `ExportWindow_Container`, nhưng chưa đọc được nút/ô nhập bên trong qua UI Automation. Adapter mới dùng UI Automation cho cửa sổ, Windows OCR cho chữ và Win32 SendInput cho chuột/bàn phím. Project được tìm bằng OCR trong khung nhìn hiện tại trước khi cuộn. Đối với danh sách, bộ nhận diện kiểm tra tên, cột dung lượng và thời lượng trên cùng một hàng rồi chọn khoảng trống trong cột Tên. Đối với lưới, cần tiêu đề vùng Dự án hoặc metadata dung lượng/thời lượng ngay dưới tên; thumbnail nằm phía trên tên. Vì thế không chỉ dựa vào việc đọc được một chuỗi tên để bấm thẻ AI; các ô xuất dùng bố cục cột bên phải. Trước khi double-click hàng/thumbnail, ứng dụng di chuyển con trỏ, chờ 1,2 giây và đọc lại tên/vị trí để dừng nếu bố cục đổi. Tìm kiếm có giới hạn 50 lần quét hoặc thời gian chờ mở project; danh sách quá lớn có thể cần hiệu chỉnh thời gian/chế độ tìm. Đây là adapter theo bố cục, chưa thích ứng mọi cách tùy biến CapCut. OCR không khớp/không duy nhất hoặc đọc lại giá trị thất bại sẽ dừng. Chưa hỗ trợ chọn thư mục qua hộp thoại riêng nếu ô Xuất sang không cho nhập trực tiếp. Chưa xác nhận thao tác đóng editor sẽ trở về trang chủ trên PC đích; nếu không về được sẽ giữ công việc đã xuất ở trạng thái Hoàn tất và dừng hàng đợi.

Tên project được so khớp không dấu/khoảng trắng/ký tự phân cách, nhưng không fuzzy-match hoặc đoán tên gần giống. Các tên không phân biệt được sau chuẩn hóa bị từ chối. Tên phải hiển thị đủ trên card và thanh tiêu đề; tên bị cắt ngắn có thể không nhận diện được.

Chưa hỗ trợ hộp thoại tài nguyên thiếu, Pro, ghi đè, lỗi render hoặc đăng nhập; gặp chúng sẽ dừng/timeout. Không thay đổi tệp nội bộ của project, không dùng API/CLI xuất CapCut chưa được xác nhận. Tên và version trong profile phải khớp môi trường thực tế.

## Phát triển

```powershell
dotnet test tests/AutoRender.Tests/AutoRender.Tests.csproj -c Release
dotnet build src/AutoRender.App/AutoRender.App.csproj -c Release
dotnet run --project src/AutoRender.App/AutoRender.App.csproj -c Release
dotnet publish src/AutoRender.App/AutoRender.App.csproj -c Release -r win-x64 --self-contained true -o artifacts/win-x64
```

Core và kiểm thử chạy được trên Linux. WPF chỉ chạy trên Windows; cross-build không chứng minh việc xuất CapCut hoạt động. Dùng thư mục dự án hiện có; không cần tạo Git worktree.
