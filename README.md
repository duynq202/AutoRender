# AutoRender — CapCut 9.6.0 tiếng Việt

Ứng dụng Windows WPF/.NET 10 để xuất tuần tự project CapCut đã có sẵn theo danh sách tên. Thiết kế mới hoàn toàn bằng C#, không dùng Python hoặc giao diện cũ.

**Trạng thái: đã triển khai hàng đợi, giao diện, lưu trạng thái và driver UI Automation có kiểm tra lỗi. Chưa kiểm chứng tích hợp CapCut trên Windows. Profile nhận diện UI còn trống; chưa thể tự động xuất trước khi khảo sát và hiệu chỉnh profile trên PC đích.** Không có tọa độ hoặc nhãn nút được đoán sẵn để giả định tương thích CapCut 9.6.0.

## Chạy trên Windows

Bản đóng gói: giải nén toàn bộ `AutoRender-win-x64.zip` rồi mở `AutoRender.App.exe`. Giữ các thư viện và thư mục `profiles` cạnh EXE. Bản này dành cho Windows x64 và kèm .NET runtime.

Từ mã nguồn: cài .NET 10 SDK, chạy `start.bat`. CapCut 9.6.0 tiếng Việt phải được cài sẵn, chạy cùng mức quyền với AutoRender, desktop mở khóa.

## Quy trình

1. Thực hiện bước khảo sát trong [docs/CAPCUT-PROFILE.md](docs/CAPCUT-PROFILE.md). Bản hiện tại sẽ từ chối chạy profile chưa được hiệu chỉnh và kiểm chứng.
2. Chọn đường dẫn `CapCut.exe` và profile đã hoàn thiện.
3. Chọn thư mục đầu ra, độ phân giải (mặc định 1080P), FPS (mặc định 30) và thời gian chờ tối đa mỗi video (mặc định 720 phút). Chỉ chọn thông số có thật trong CapCut trên máy.
4. Chuẩn bị file `.txt` UTF-8: mỗi dòng là **tên chính xác, duy nhất** của một project có sẵn. Ví dụ trong `examples/projects.txt`. Bỏ dòng trống; không tự sắp xếp hoặc bỏ tên trùng. Tên trùng bị từ chối. Tên không hợp lệ làm tên file Windows cũng bị từ chối thay vì tự đổi tên.
5. Chọn **Nhập danh sách .txt**. Thứ tự trong file là thứ tự xuất. Cấu hình xuất được lưu cùng hàng đợi. Muốn thay cấu hình, nhập lại danh sách và xác nhận thay hàng đợi.
6. Xác nhận project đã có canvas **16:9**. Ứng dụng giữ bố cục sẵn có, không cắt/căn lại nội dung và không tự đổi canvas.
7. Giữ CapCut ở trang chủ. Nhấn **Chạy / Tiếp tục**. Driver tìm đúng project theo tên, mở hộp xuất, đặt tên file, thư mục, độ phân giải, FPS và MP4; đọc lại giá trị/chọn lựa trước khi xuất.
8. Mỗi video có tên `<tên project>.mp4` trong thư mục đầu ra. Chỉ chuyển tiếp khi CapCut báo thành công **và** file đầu ra có dữ liệu, có thể mở độc quyền để đọc. Đây chưa phải kiểm tra giải mã toàn bộ nội dung video; hãy thử với video ngắn và kiểm tra kết quả thủ công trước.

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

Driver hiện dùng Windows UI Automation trực tiếp, không cần thêm lớp thư viện FlaUI. Nó yêu cầu CapCut cung cấp các pattern `Invoke`, `Value`, `SelectionItem` tương ứng và cho tìm project qua ô tìm kiếm. Chưa có fallback OCR/nhận diện ảnh, cuộn danh sách hoặc thao tác bàn phím. Nếu khảo sát cho thấy UI không hỗ trợ các pattern này, cần triển khai adapter khác; không thể chỉ điền profile để khắc phục.

Chưa hỗ trợ hộp thoại tài nguyên thiếu, Pro, ghi đè, lỗi render hoặc đăng nhập; gặp chúng sẽ dừng/timeout. Không thay đổi tệp nội bộ của project, không dùng API/CLI xuất CapCut chưa được xác nhận. Tên và version trong profile phải khớp môi trường thực tế.

## Phát triển

```powershell
dotnet test tests/AutoRender.Tests/AutoRender.Tests.csproj -c Release
dotnet build src/AutoRender.App/AutoRender.App.csproj -c Release
dotnet run --project src/AutoRender.App/AutoRender.App.csproj -c Release
dotnet publish src/AutoRender.App/AutoRender.App.csproj -c Release -r win-x64 --self-contained true -o artifacts/win-x64
```

Core và kiểm thử chạy được trên Linux. WPF chỉ chạy trên Windows; cross-build không chứng minh việc xuất CapCut hoạt động. Dùng thư mục dự án hiện có; không cần tạo Git worktree.
