# Khảo sát CapCut trước khi hoàn thiện adapter

Mẫu `profiles/capcut-9.6.vi.template.json` cố ý để trống selector, với `VerifiedOnThisPc=false`. Không đặt cờ thành true để bỏ qua bước kiểm chứng.

## Thu thập trên PC Windows

1. Mở AutoRender, chọn `CapCut.exe`, mở CapCut 9.6.0 tiếng Việt ở trang chủ.
2. Nhấn **Khảo sát UI CapCut**, lưu thành `01-home.json`.
3. Tự mở một project ngắn, nhấn khảo sát và lưu `02-editor.json`.
4. Mở hộp thoại xuất, nhấn khảo sát và lưu `03-export.json`. Nếu cần, khảo sát thêm khi mở danh sách độ phân giải/FPS/định dạng.
5. Xuất thử thủ công, chờ thông báo thành công, khảo sát và lưu `04-success.json`.

Khảo sát chỉ đọc cây UI, không bắt đầu xuất hay sửa nội dung project. Nó có thể mở CapCut nếu chưa chạy và đưa cửa sổ ra phía trước. File JSON chứa Name, AutomationId, ControlType, pattern được hỗ trợ và vị trí phần tử. `Truncated=true` nghĩa là có phần cây chưa được thu thập; không kết luận một control không tồn tại chỉ dựa vào bản khảo sát bị cắt.

Gửi các file này cho người phát triển để xác định có thể dùng UI Automation hay cần adapter nhận diện ảnh. Các file phản ánh màn hình hiện tại; cần khảo sát từng trạng thái như trên.

## Điền profile khi đủ khả năng UI Automation

Sao chép mẫu sang một file riêng. Dùng giá trị từ khảo sát, không dịch hoặc đoán tên. Một selector gồm `Name`, `AutomationId`, `ControlType` (ví dụ `Button`, bỏ tiền tố `ControlType.`). Cần Name hoặc AutomationId; khi cung cấp nhiều thuộc tính thì tất cả phải khớp. Control phải hiển thị và khớp duy nhất trong phạm vi tìm kiếm.

| Key | Ý nghĩa / yêu cầu |
|---|---|
| search | Ô tìm project ở trang chủ; ValuePattern cho phép nhập |
| editorMarker | Phần tử riêng chỉ có trong trình biên tập, dùng xác nhận mở xong |
| openExport | Nút mở hộp xuất; InvokePattern |
| dialog | Container hộp xuất; dùng giới hạn các selector bên trong |
| fileName | Ô tên file trong dialog; ValuePattern |
| folder | Ô thư mục đầu ra trong dialog; ValuePattern |
| resolution, fps, format | Control chọn cấu hình; ExpandCollapse hoặc Invoke; các mục cần SelectionItem |
| confirmExport | Nút bắt đầu xuất trong dialog; InvokePattern |
| success | Phần tử chỉ xuất hiện khi export thành công, không phải nhãn phần trăm |
| closeSuccess | Nút đóng thông báo thành công; InvokePattern |
| home | Nút về trang chủ từ editor; InvokePattern |

`ProjectControlType` phải trỏ đến card project có tên đúng và hỗ trợ Invoke. Driver kiểm tra lại tên project và editorMarker sau khi mở. Không tự đi lên các phần tử cha hoặc click vị trí đoán được. Nếu card không hỗ trợ Invoke, adapter hiện tại chưa dùng được.

`ResolutionLabels`, `FpsLabels`, `Mp4Label` là nhãn **thực tế** trong menu CapCut. Các giá trị của mẫu chưa được xác nhận. `FileNameIncludesExtension` xác định ô tên file cần nhập `.mp4` hay không. Dùng cùng thư mục đầu ra đang chọn trong AutoRender.

Sau khi người phát triển đối chiếu đầy đủ selector/pattern và thử một project ngắn, mới lưu profile đã xác nhận với `VerifiedOnThisPc=true`. Cờ này ghi lại bước kiểm chứng của người dùng; không chứng minh tự động tương thích. Khi đổi CapCut, ngôn ngữ hoặc giao diện, cần kiểm chứng lại.
