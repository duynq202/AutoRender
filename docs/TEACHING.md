# Học ảnh và điểm bấm trên PC

Ảnh mẫu phải lấy từ đúng phiên bản/ngôn ngữ/DPI CapCut của bạn. Không copy ảnh tiếng Anh trong app tham khảo rồi giả định khớp tiếng Việt.

## Cách học một mục

1. Mở đúng màn hình CapCut chứa mục đó. Trong RenderBatch chọn mục trong danh sách rồi bấm **Học ảnh / điểm bấm**. Công cụ đưa CapCut lên trước và chụp cửa sổ. Học ảnh không bấm vào nút thật.
2. Trên ảnh tĩnh, kéo một hình chữ nhật nhỏ quanh biểu tượng/chữ **cố định**, có thêm chút nền; vùng tối thiểu 8×8. Không chỉ chọn mảng màu phẳng hoặc tên project/phần trăm/đường dẫn đang thay đổi.
3. Thả chuột, rồi bấm một lần vào vị trí cần thao tác trên ảnh. Điểm này được ghi tương đối với ảnh nhận diện, có thể nằm ngoài vùng kéo. Ví dụ: nhận diện nhãn Tên và ghi điểm bấm trong ô tên bên phải.
4. Lưu. Công cụ kiểm tra ảnh khớp chính ảnh gốc và không trùng nhiều nơi trước khi thay cấu hình cũ.
5. Bấm **Tìm ảnh, chỉ trỏ chuột** với cùng mục. Chỉ khi con trỏ đúng điểm mới dùng trong hàng đợi. Nếu thay bố cục/kích thước/DPI, học lại.

## Thứ tự và mục cần học

| Mục | Màn hình / ảnh nhận diện | Điểm bấm |
|---|---|---|
| Trang chủ: dấu hiệu vùng dự án | Trang chủ, nhãn Dự án cố định | Tâm nhãn (chỉ dùng chờ) |
| Nút mở tìm kiếm | Trang chủ, icon kính lúp của danh sách project | Tâm nút |
| Ô nhập tìm kiếm (đã mở) | Mở tìm kiếm thủ công; chọn phần viền/icon không đổi, tránh placeholder/tên đã nhập | Bên trong ô gõ |
| Nhãn Tên → hàng kết quả đầu tiên | Chế độ danh sách; chọn nhãn Tên của bảng | Khoảng trống trong cột tên ở hàng đầu tiên. Bản mới bấm một lần như app tham khảo |
| Nút Xuất trong editor | Editor | Nút Xuất góc trên phải |
| Dấu hiệu hộp cấu hình xuất | Hộp xuất; nhãn Độ phân giải hoặc nhãn cố định khác | Tâm nhãn (chỉ dùng chờ) |
| Nhãn Tên → ô tên file | Hộp xuất, nhãn Tên | Ô nhập tên bên phải |
| Nút mở hộp chọn thư mục | Hộp xuất, icon thư mục cạnh Xuất sang | Tâm icon |
| Nhãn Xuất sang → ô đường dẫn | Hộp xuất; nhãn cố định, không chọn chuỗi đường dẫn | Giữa ô full path để nhập hoặc Ctrl+A/Ctrl+C đọc lại |
| Nhãn độ phân giải → menu | Hộp xuất, nhãn Độ phân giải | Ô chọn bên phải |
| Giá trị độ phân giải đang chọn | Chọn đúng 720p/1080p/4k thủ công, đóng menu rồi kéo vùng chữ giá trị gọn, tránh viền/arrow | Tâm giá trị. Ảnh sẽ dùng tìm cả lựa chọn trong menu và xác minh trạng thái đã chọn |
| Nhãn FPS → menu | Hộp xuất, nhãn Tỷ lệ khung hình/FPS | Ô chọn FPS |
| Giá trị FPS đang chọn | Chọn 30fps/60fps thủ công, đóng menu; kéo vùng chữ giá trị gọn | Tâm giá trị |
| Nhãn định dạng → menu | Hộp xuất, nhãn Định dạng | Ô chọn định dạng |
| Giá trị MP4 đã chọn | Chọn MP4, menu đóng; kéo vùng chữ mp4 gọn | Tâm giá trị |
| Nút bắt đầu xuất trong hộp xuất | Nút Xuất phía dưới hộp xuất | Tâm nút |
| Thông báo Đã lưu video | Sau khi xuất thử thủ công: đoạn chữ xác nhận đã lưu (không chọn Đang tạo/phần trăm) | Tâm chữ (chỉ dùng chờ) |
| Nút đóng thông báo thành công | Màn hình thành công | Nút Đóng, không phải Chia sẻ |

Đổi độ phân giải/FPS trong RenderBatch sẽ đưa thêm mục giá trị cần học vào danh sách. Chỉ cần học các cấu hình bạn sử dụng. Chế độ nhập full path không bắt buộc ảnh icon chọn thư mục.

## Điều kiện để ảnh đáng tin cậy

Ngưỡng tương đồng mặc định 0.97. Đây là hệ số template matching, không phải xác suất thao tác đúng. Có kiểm tra ảnh nền phẳng, khớp trùng, thay đổi vị trí trước lúc bấm và focus. Tìm sai hoặc không khớp thì không tự lấy tọa độ gần nhất.

Chỉ dạy giá trị đã chọn bằng vùng chữ nhỏ, không chọn cả menu. Nếu ảnh trong menu khác kiểu chữ/màu quá nhiều và không khớp, cần thêm biến thể ảnh; bản đầu chưa có UI nhiều biến thể. Nhật ký và ảnh lỗi nằm ở thư mục ứng dụng trong LocalAppData để chẩn đoán. Chưa có kiểm chứng end-to-end trên PC đích.
