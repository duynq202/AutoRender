# AutoRender — xuất tuần tự project CapCut trên Windows

Bản đầu tiên điều khiển chuột trên CapCut Desktop: chọn project trên trang chủ → mở hộp xuất → xác nhận → đợi ảnh thông báo thành công → đóng thông báo → về trang chủ → chọn project tiếp theo.

## Cài và chạy

Cần Windows 10/11, Python 3.11 trở lên (có Tkinter), CapCut Desktop đã đăng nhập và desktop đang mở khóa. Nhấp đúp `start.bat`; lần đầu cần Internet để cài thư viện. Không cần cung cấp mật khẩu hay token cho tool.

## Hiệu chỉnh trước khi chạy

1. Sao lưu project và thử xuất thủ công một project ngắn. Thiết lập thư mục xuất, chất lượng và tên file trong CapCut. Tool dùng thiết lập hiện tại của CapCut; chưa tự đặt tên file hoặc xử lý hộp thoại ghi đè.
2. Chụp thông báo **xuất thành công** bằng Snipping Tool, cắt riêng một vùng nhỏ, cố định, có nội dung báo thành công; lưu PNG rồi chọn bằng nút **Chọn ảnh thành công**. Không dùng ảnh thanh tiến trình hay chỉ một mảng màu. So khớp ảnh là chính xác từng pixel; giữ nguyên ngôn ngữ, theme, DPI và kích thước cửa sổ.
3. Dùng **Lấy tọa độ sau 5s**, chuyển sang CapCut rồi đưa chuột tới nút cần lấy. Nhập tọa độ được ghi trong nhật ký vào cấu hình JSON:
   - `projects`: tên để ghi nhật ký và `position` của từng ô project ở trang chủ. Chỉ dùng các ô đang nhìn thấy, không cuộn hoặc sắp xếp lại trong lúc chạy.
   - `export_button`: nút mở hộp xuất trong trình biên tập.
   - `confirm_export_button`: nút bắt đầu xuất trong hộp xuất.
   - `close_completion_button`: nút đóng thông báo thành công.
   - `home_button`: nút trở về trang chủ từ trình biên tập.
4. Điều chỉnh các khoảng chờ nếu máy chậm. `export_timeout_seconds` là thời gian tối đa cho mỗi project. `completion_region` có thể để `null`, hoặc `[x, y, width, height]` để giới hạn vùng tìm ảnh trên màn hình chính.
5. Lưu cấu hình ngoài mã nguồn (ví dụ `config.local.json`). Mở trang chủ CapCut, giữ cửa sổ tối đa trên màn hình chính, rồi nhấn **Chạy hàng đợi**. Có 5 giây để chuyển sang CapCut. Thử một project trước khi chạy nhiều project.

Không sử dụng chuột/bàn phím, khóa màn hình hoặc thay đổi cửa sổ khi tool chạy. Nút **Dừng** ngừng các thao tác tiếp theo; không hủy tác vụ xuất đang chạy trong CapCut. Đưa chuột tới góc trên bên trái màn hình để kích hoạt dừng khẩn cấp của PyAutoGUI. Khi timeout/lỗi, kiểm tra trạng thái CapCut trước khi chạy lại; hàng đợi chạy lại từ đầu.

## Giới hạn hiện tại

Đây là tự động hóa theo tọa độ đã hiệu chỉnh, chưa có nhận diện danh sách project, cuộn danh sách, xác nhận đúng project hoặc kiểm tra nội dung file video. Tool nhận diện thông báo thành công trên màn hình; người dùng cần kiểm tra file đầu ra. Hộp thoại ghi đè, thiếu media, yêu cầu Pro hoặc lỗi xuất sẽ cần xử lý thủ công. Vị trí và giao diện CapCut khác phiên bản phải hiệu chỉnh lại. Chưa kiểm thử tích hợp trên máy Windows có CapCut thực tế.

## Phát triển và kiểm thử

```powershell
py -3 -m venv .venv
.venv\Scripts\python.exe -m pip install -r requirements.txt
.venv\Scripts\python.exe -m unittest discover -s tests -v
.venv\Scripts\python.exe autorender.py
```

Linux cloud có thể chạy kiểm thử logic hàng đợi với desktop giả lập; không chạy xuất CapCut thực tế. Dùng checkout có sẵn, không cần tạo Git worktree.
