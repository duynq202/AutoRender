# Phân tích tĩnh RenderCapcutGUI.exe

File tham khảo được người dùng cung cấp và tải trực tiếp từ Google Drive. Không chạy EXE hoặc thực thi bytecode trích xuất; chỉ phân tích PE, danh mục PyInstaller, hằng chuỗi, tên hàm và disassembly của code object bằng Python đúng phiên bản.

- Kích thước: 68.001.846 byte.
- SHA-256: `5fb5af7868d08535e3133198ab99a9dfb2a0a5263383abaa592a0afb3a3fbffb`.
- PE32+ Windows GUI x64, không có CLR directory.
- PyInstaller, Python 3.13 (`python313.dll`), 1.369 mục trong archive.
- GUI: `render_capcut_gui`, Tkinter/ttk.
- Lõi: module `render_capcut`, OpenCV, NumPy, MSS, Pillow, PyAutoGUI, pygetwindow.
- Có 12 ảnh mẫu đóng gói, trong đó `search.png`, `display-search.png`, `name.png`, `export.png`, `export-new.png`, `export-2.png`, `exporting.png`, `cancel.png`, `close.png` xuất hiện trong các bước điều khiển. Ảnh `name.png`, `exporting.png`, `cancel.png` thể hiện giao diện tiếng Anh.

## Luồng đã xác minh trong bytecode

1. Khởi chạy CapCut, tìm biểu tượng tìm kiếm bằng ảnh `search.png` với ngưỡng tương đồng 0.97.
2. Kiểm tra `display-search.png` rồi gõ tên project.
3. Tìm nhãn Name qua `name.png`, bấm một lần cách nhãn 50 pixel theo chiều dọc để mở kết quả đầu tiên. Đợi editor và nút xuất.
4. Tìm nút xuất theo `export.png`/`export-new.png`, mở hộp xuất, tìm `export-2.png` và bắt đầu render.
5. Tiếp tục chờ khi còn thấy `exporting.png`. Khi không thấy ảnh này nữa thì tiếp tục thao tác đóng.
6. Luồng điều phối gọi `taskkill /F /IM CapCut.exe` trước/sau các project. Có lựa chọn tắt máy bằng lệnh shutdown khi xong.

## Những giới hạn không nên coi là đã giải quyết

- Vùng chụp cố định 3840 × 1080 không tương ứng mọi PC/màn hình.
- Ảnh mẫu tiếng Anh không tự tương thích CapCut tiếng Việt hoặc DPI khác.
- Kết quả đầu tiên của tìm kiếm có thể là tên gần giống; code đọc được không xác minh lại tên project trong hộp xuất.
- Vắng ảnh Exporting không chứng minh thành công; popup lỗi/thay đổi bố cục cũng có thể làm mất ảnh.
- Luồng render đã đọc không đặt thư mục đầu ra, độ phân giải hoặc FPS. Có ảnh 4K trong gói không có nghĩa luồng đã chọn được 4K.
- Hàm get_images_path trỏ tới images cạnh chương trình, trong khi ảnh cũng đóng gói trong archive PyInstaller. EXE đơn lẻ có thể vẫn thiếu thư mục ảnh ngoài như đường dẫn code tìm.
- Hàm GUI stop_render đã đọc chỉ xác nhận/ghi thông báo; không truyền tín hiệu hủy tới vòng lặp render.
- Gửi tên bằng pyautogui.write có giới hạn với Unicode. Dừng cưỡng bức CapCut có thể làm gián đoạn công việc khác.

## Hướng triển khai mới

Giữ WPF theo yêu cầu trước của người dùng, dùng C#/.NET 10 + OpenCvSharp. Viết mới luồng và giao diện, không đưa bytecode/app tham khảo vào bản phân phối. Dùng ảnh người dùng học từ CapCut thực tế, ngưỡng 0.97, từ chối nền phẳng/khớp trùng, giới hạn tìm trong cửa sổ và vùng đã học. Không OCR cho nút/tên project.

Dùng metadata cửa sổ mà các lần khảo sát trước đã xác minh: HomeWindow, MainWindow, ExportWindow_Container. Đối chiếu tiêu đề `Xuất-<tên project>` trước khi xuất để ngăn xuất nhầm kết quả đầu tiên. Xác nhận đường dẫn bằng clipboard read-back. Chỉ hoàn tất khi có ảnh báo thành công và MP4 có dữ liệu/không còn bị giữ mở. Không taskkill hoặc shutdown. Có hủy thật và lưu trạng thái để kiểm tra sau gián đoạn.

Đây là các kết luận từ phân tích tĩnh. Chưa chứng minh app gốc hoặc bản viết lại export thành công trên PC Windows có CapCut.
