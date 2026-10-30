# RenderBatch 1.1.2 — xuất CapCut theo vòng lặp 6 ảnh

C#/.NET 10 + WPF + OpenCvSharp. Tham khảo cơ chế nhận diện ảnh của RenderCapcutGUI.exe; [báo cáo phân tích](docs/REFERENCE-ANALYSIS.md). Không sử dụng OCR.

**Trạng thái:** đã kiểm thử lõi và build gói Windows. Cần học ảnh trên PC của bạn và thử một project ngắn; chưa kiểm chứng toàn bộ luồng trên Windows có CapCut.

## Quy trình mặc định

Trang chủ CapCut → icon tìm kiếm → nhập tên ngay → mở project → chờ 60 giây → nút Xuất trong editor → nút bắt đầu xuất → xác nhận đã lưu và file video ghi xong → đóng CapCut → mở lại, chờ trang chủ → project tiếp theo.

Chỉ cần học **6 ảnh**, không cần ảnh ô nhập tìm kiếm. Icon tìm kiếm tự đặt focus vào ô nhập; ứng dụng gõ tên và đọc lại để kiểm tra ngay sau khi bấm icon. Bước đóng/mở lại dùng Windows, không cần ảnh nút Đóng. Sau project cuối cũng mở lại trang chủ. Ứng dụng gửi yêu cầu đóng bình thường; nếu CapCut không thoát sau 45 giây (ví dụ còn hộp thoại), hàng đợi dừng và giữ video đã hoàn tất. Không tự tắt cưỡng bức khi đang render.

## Chạy lần đầu

1. Giải nén **toàn bộ** gói Windows x64, mở `RenderBatch.Windows.exe`. Không cần cài Python hoặc .NET SDK. Cần CapCut 9.6.0 và desktop mở khóa.
2. Chọn đường dẫn `CapCut.exe` để có thể mở lại sau mỗi video.
3. Trong CapCut, đặt sẵn **thư mục lưu, MP4, độ phân giải, FPS** (ví dụ 1080p/30fps). Tên file xuất phải trùng tên project. Chuẩn bị canvas 16:9 từ trước. Chạy thử thủ công để xác nhận CapCut giữ thiết lập sau khi đóng/mở lại.
4. Trong RenderBatch chọn **cùng thư mục video**. Ở chế độ gọn, đây là thư mục theo dõi file; ứng dụng dùng các thiết lập hiện tại của CapCut, không tự đặt lại thư mục, tên hay thông số xuất.
5. Mở danh sách project ở **chế độ danh sách**, để ô tìm kiếm đang đóng trước khi bắt đầu hàng đợi. Học 6 ảnh theo [hướng dẫn](docs/TEACHING.md), sau đó dùng **Tìm ảnh, chỉ trỏ chuột** để kiểm tra vị trí, nhất là hàng kết quả đầu tiên.
6. Nhập file `.txt` UTF-8, mỗi dòng một tên project chính xác; bấm **Chạy / Tiếp tục**. Thử một project ngắn trước khi chạy danh sách dài.

## Xác nhận trước khi sang project tiếp theo

Tên tìm kiếm được gõ Unicode và đọc lại để kiểm tra ngay sau khi bấm icon; không tìm hoặc bấm ảnh ô nhập. Sau khi bấm project, ứng dụng chờ **đủ 60 giây** để project nặng tải dữ liệu, sau đó tiếp tục chờ nút Xuất tối đa 180 giây theo cấu hình mặc định. Không đóng CapCut vì chưa thấy nút Xuất trong khoảng chờ này. Nếu hết thời gian vẫn chưa thấy nút, hàng đợi dừng để kiểm tra, không tự đóng app. Nút Dừng và góc dừng khẩn cấp vẫn hoạt động trong 60 giây chờ. Sau lọc, ứng dụng bấm hàng đầu tiên, rồi so sánh chính xác tiêu đề hộp Xuất với tên yêu cầu. Tên gần giống có thể làm CapCut trả về nhiều kết quả; nếu mở nhầm, hàng đợi dừng trước khi bắt đầu xuất, chưa tự duyệt các hàng còn lại.

Hoàn tất cần cả ảnh **Đã lưu video** và file `<tên project>.mp4` có dữ liệu, mở độc quyền được. Chỉ ảnh thành công hoặc chỉ file tồn tại đều chưa đủ. Nếu CapCut dùng tên khác, thêm hậu tố hoặc lưu sang thư mục khác, RenderBatch tiếp tục chờ đến hạn và không đóng CapCut/chạy project tiếp theo. File đã có trong thư mục theo dõi bị chặn từ trước; ứng dụng không xử lý nút xác nhận ghi đè của CapCut. Chế độ gọn không đọc lại đường dẫn trong hộp xuất trước khi chạy.

Ảnh được tìm trong vùng đã học của cửa sổ CapCut. Sai DPI/kích thước, ảnh trùng hoặc mất focus thì dừng. Giữ cùng bố cục và kích thước khi mở lại. Ngưỡng 0.97 là hệ số khớp ảnh, không phải xác suất đúng.

## Chờ xuất video dài

Chờ mở project **60 giây**, sau đó dò nút Xuất tối đa 180 giây mặc định. Từ khi bắt đầu xuất video, giới hạn chờ mặc định và tối đa là **180 phút**. Cứ đến mỗi mốc 1 phút, ứng dụng quét ảnh Đã lưu video và kiểm tra file MP4 tương ứng. Lần quét cuối ở phút 180 vẫn được nhận thành công nếu đủ điều kiện.

Quá trình quét định kỳ không ghi thêm log lên giao diện. Nhật ký chỉ ghi các bước thao tác, hoàn tất, dừng hoặc lỗi. Nút Dừng và góc dừng khẩn cấp vẫn hoạt động giữa các lần quét. Hết hạn chưa xác nhận được video thì dừng hàng đợi, giữ CapCut mở và không chuyển project tiếp theo.

Cấu hình 720 phút từ phiên bản trước được hạ xuống 180 phút khi mở bản mới, vẫn giữ profile và trạng thái project. Có thể nhập thời gian ngắn hơn, từ 1 đến 180 phút, trong ô Chờ xuất.

## Khi cần tự đặt cấu hình xuất

Bỏ chọn **Quy trình gọn** để hiện độ phân giải/FPS và các ảnh bổ sung. Chế độ này đặt tên, thư mục, MP4, độ phân giải và FPS, đọc lại đường dẫn trước khi xuất. Dùng hộp chọn thư mục chuẩn Windows, hoặc bỏ chọn hộp Windows để nhập full path nếu CapCut cho phép. Hộp chọn tùy biến Qt chưa được hỗ trợ. Xuất xong vẫn đóng/mở lại CapCut.

## Dừng và tiếp tục

Nút **Dừng** hoặc đưa chuột về góc trên trái hủy các thao tác tiếp theo, không hủy render đang chạy. Không dùng chuột/bàn phím hay khóa desktop trong khi xuất tự động.

Cấu hình, ảnh, trạng thái và nhật ký ở `%LOCALAPPDATA%\RenderBatchTemplates`. Có thể dùng lại 6 ảnh tương ứng đã học từ bản 1.0 nếu bố cục/DPI giữ nguyên; không cần xóa profile cũ. Job bị gián đoạn được đánh dấu **Cần kiểm tra**; video hoàn tất vẫn được giữ nếu khởi động lại CapCut lỗi. Job hoàn tất được bỏ qua khi tiếp tục cùng danh sách/cấu hình.

## Phát triển

Cài .NET 10 SDK để chạy source bằng `start.bat`, hoặc:

```powershell
dotnet test tests/RenderBatch.Tests/RenderBatch.Tests.csproj -c Release
dotnet build src/RenderBatch.Windows/RenderBatch.Windows.csproj -c Release
dotnet publish src/RenderBatch.Windows/RenderBatch.Windows.csproj -c Release -r win-x64 --self-contained true -o artifacts/windows
```

Chạy test và build tuần tự do dùng chung output core. Linux chạy được kiểm thử OpenCV native; WPF chỉ chạy trên Windows. Gói Windows loại plugin `opencv_videoio_ffmpeg*.dll` không dùng đến, giữ OpenCV nhận diện ảnh và .NET runtime. CapCut thực hiện render video. Cấu hình lựa chọn repository của môi trường được giữ nguyên.
