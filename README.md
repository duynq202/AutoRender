# RenderBatch 1.1.9 — xuất CapCut theo vòng lặp 7 ảnh

C#/.NET 10 + WPF + OpenCvSharp. Tham khảo cơ chế nhận diện ảnh của RenderCapcutGUI.exe; [báo cáo phân tích](docs/REFERENCE-ANALYSIS.md). Không sử dụng OCR.

**Trạng thái:** đã kiểm thử lõi và build gói Windows. Cần học ảnh trên PC của bạn và thử một project ngắn; chưa kiểm chứng toàn bộ luồng trên Windows có CapCut.

## CapCut 9.7.0

Bản 1.1.9 cho phép kết nối CapCut 9.7.0 bên cạnh 9.6.0, gồm các chuỗi phiên bản có hậu tố build như `9.7.0.123`. Các phiên bản khác vẫn bị chặn.

Đây là cập nhật điều kiện nhận phiên bản; chưa kiểm chứng thao tác GUI trên CapCut 9.7.0 thực tế. Khi nâng từ 9.6 lên 9.7, học lại các ảnh có giao diện thay đổi, kiểm tra điểm bấm bằng chế độ chỉ trỏ chuột và thử một project ngắn. Giữ nguyên profile cũ để có thể dùng lại, không tự chuyển đổi ảnh cũ thành ảnh 9.7.0. Nếu cửa sổ/nhãn thay đổi và không nhận diện được, ứng dụng dừng để kiểm tra.

## Quy trình mặc định

Trang chủ CapCut → icon tìm kiếm → nhập tên ngay → mở project → thử nút Xuất mỗi 5 giây, tối đa 120 giây → bảng xuất → nút bắt đầu xuất → nhận diện hoàn tất và file video ghi xong → bấm Đóng → đóng CapCut → mở lại, chờ trang chủ → project tiếp theo.

Chỉ cần học **7 ảnh**, không cần ảnh ô nhập tìm kiếm. Icon tìm kiếm tự đặt focus vào ô nhập; ứng dụng gõ tên và đọc lại để kiểm tra ngay sau khi bấm icon. Sau khi xác nhận video đã ghi xong, bấm ảnh nút Đóng rồi dùng Windows đóng/mở lại CapCut. Sau project cuối cũng mở lại trang chủ. Ứng dụng gửi yêu cầu đóng bình thường; nếu CapCut không thoát sau 45 giây (ví dụ còn hộp thoại), hàng đợi dừng và giữ video đã hoàn tất. Không tự tắt cưỡng bức khi đang render.

## Chạy lần đầu

1. Giải nén **toàn bộ** gói Windows x64, mở `RenderBatch.Windows.exe`. Không cần cài Python hoặc .NET SDK. Cho phép kết nối CapCut 9.6.0 / 9.7.0, giao diện tiếng Việt và desktop mở khóa.
2. Chọn đường dẫn `CapCut.exe` để có thể mở lại sau mỗi video.
3. Trong CapCut, đặt sẵn **thư mục lưu, MP4, độ phân giải, FPS** (ví dụ 1080p/30fps). Tên file xuất phải trùng tên project. Chuẩn bị canvas 16:9 từ trước. Chạy thử thủ công để xác nhận CapCut giữ thiết lập sau khi đóng/mở lại.
4. Trong RenderBatch chọn **cùng thư mục video**. Ở chế độ gọn, đây là thư mục theo dõi file; ứng dụng dùng các thiết lập hiện tại của CapCut, không tự đặt lại thư mục, tên hay thông số xuất.
5. Mở danh sách project ở **chế độ danh sách**, để ô tìm kiếm đang đóng trước khi bắt đầu hàng đợi. Học 7 ảnh theo [hướng dẫn](docs/TEACHING.md), sau đó dùng **Tìm ảnh, chỉ trỏ chuột** để kiểm tra vị trí, nhất là hàng kết quả đầu tiên.
6. Nhập file `.txt` UTF-8, mỗi dòng một tên project chính xác; bấm **Chạy / Tiếp tục**. Thử một project ngắn trước khi chạy danh sách dài.

## Xác nhận trước khi sang project tiếp theo

Tên tìm kiếm được gõ Unicode và đọc lại ngay sau khi bấm icon. Sau lọc, ứng dụng mở hàng đầu tiên, rồi thử mở bảng xuất ngay, mỗi 5 giây một lần trong tối đa 120 giây. Khi bảng đã xuất hiện thì dừng bấm nút Xuất trong editor. Trước khi bắt đầu render, tiêu đề bảng xuất phải khớp chính xác tên project. Nếu tên gần giống làm mở nhầm project, hàng đợi dừng trước khi render; chưa tự duyệt các hàng còn lại.

Hoàn tất cần ảnh **nút Đóng riêng hoặc cặp nút Chia sẻ và Đóng** trên bảng hoàn tất, cùng file `<tên project>.mp4` có dữ liệu, mở độc quyền được. Chỉ ảnh thành công hoặc chỉ file tồn tại đều chưa đủ. Nếu CapCut dùng tên khác, thêm hậu tố hoặc lưu sang thư mục khác, RenderBatch tiếp tục chờ đến hạn và không đóng CapCut/chạy project tiếp theo. File đã có trong thư mục theo dõi bị chặn từ trước; ứng dụng không xử lý nút xác nhận ghi đè của CapCut. Chế độ gọn không đọc lại đường dẫn trong hộp xuất trước khi chạy.

Ảnh được tìm trong vùng đã học của cửa sổ CapCut; riêng nút Xuất editor cho phép cửa sổ đổi kích thước và neo vùng tìm theo mép phải. DPI khác, ảnh trùng hoặc mất focus có thể làm dừng/chờ tới hạn. Giữ cùng bố cục và kích thước khi mở lại. Ngưỡng 0.97 là hệ số khớp ảnh, không phải xác suất đúng.

## Mở bảng xuất: mỗi 5 giây, tối đa 120 giây

Ngay sau khi mở project, ứng dụng thử nhận diện và bấm nút Xuất trong editor tại các mốc 0, 5, 10…115 giây, chỉ bấm khi ảnh và vị trí đã được xác minh ổn định. Giữa các lần thử, kiểm tra cửa sổ bảng xuất khoảng mỗi 0,5 giây. Dừng bấm ngay khi nhận diện cửa sổ này, kể cả lúc các trường cấu hình còn đang tải. Sau đó mới chờ nội dung bảng sẵn sàng, xác minh tên và xử lý cấu hình xuất.

Mỗi lần thử đều kiểm tra bảng xuất trước khi gửi click, dời chuột khỏi nút để bỏ trạng thái hover, tìm ảnh mới rồi xác minh lại vị trí. Nút Xuất trong editor được tìm theo mép phải/mép trên của cửa sổ, cùng DPI lúc học, cho phép editor đổi kích thước. Nếu thao tác nhận diện mất lâu, bỏ qua mốc đã lỡ và chờ mốc tiếp theo, không bấm dồn.

Hết 120 giây vẫn chưa có bảng xuất thì dừng hàng đợi, giữ CapCut mở và lưu chẩn đoán ở `%LOCALAPPDATA%\RenderBatchTemplates\diagnostics`. Nút Dừng và góc dừng khẩn cấp vẫn hoạt động giữa các lần thử. Thời gian này thay cho khoảng chờ cố định 180 giây của bản trước.

Nếu ảnh cũ được học khi hover, học lại riêng **Nút Xuất trong editor**: chọn vùng nhỏ quanh chữ/biểu tượng của nút, ghi điểm bấm ở tâm. Giữ các ảnh khác. Nếu không nhận diện được, nhật ký sẽ nêu lý do như chưa thấy editor, sai DPI hoặc ảnh chưa đạt ngưỡng.

## Tìm nút Đóng mỗi 5 giây

Sau khi bắt đầu xuất video, cứ 5 giây ứng dụng tìm ảnh **nút Đóng riêng** trước. Nếu chưa khớp, kiểm tra ảnh cặp nút Chia sẻ và Đóng đã học. Khi có một trong hai dấu hiệu và file MP4 đúng tên/thư mục đã có dữ liệu, mở độc quyền được, ứng dụng ghi nhận video hoàn tất. Sau đó vẫn tiếp tục quét ảnh nút Đóng mỗi 5 giây cho tới khi bấm được hoặc bảng đã đóng, rồi mới đóng/mở lại CapCut để sang project tiếp theo. Mỗi lần thử bấm chỉ kiểm tra ảnh hiện tại; nếu chưa khớp hoặc vị trí còn thay đổi, quay lại vòng quét 5 giây.

**Nếu đã có đủ 7 ảnh ở 1.1.8, giữ nguyên profile, không cần học lại. Nâng từ 1.1.7 cần học thêm một ảnh**, giao diện sẽ hiện 6/7 nếu các ảnh trước đã đủ:

1. Giữ bảng sau khi xuất xong đang hiển thị (như ảnh bạn gửi).
2. Trong RenderBatch chọn **Hoàn tất: nút Đóng riêng** rồi bấm **Học ảnh / điểm bấm**.
3. Khoanh gọn **chỉ nút Đóng ở góc dưới bên phải**; sau đó bấm vào chính tâm nút Đóng trên ảnh và lưu. Tránh nút Chia sẻ hoặc nút X ở thanh tiêu đề.
4. Dùng **Tìm ảnh, chỉ trỏ chuột** để kiểm tra điểm. Về trang chủ CapCut trước khi chạy hàng đợi; thử với project chưa có file đầu ra.

Ảnh mới `completion-close` được tìm trong vùng giới hạn theo khoảng cách đến mép phải và mép dưới của bảng, cùng DPI lúc học. Ảnh cặp nút `completion-actions` vẫn được giữ làm dấu hiệu dự phòng. Không chọn ảnh chỉ có nền phẳng hoặc chỉ một phần viền nút.

Chỉ bấm Đóng sau khi đã xác nhận file. Nếu chưa thấy file hoặc file còn đang được ghi, ứng dụng tiếp tục chờ và hiển thị đường dẫn cần kiểm tra trong dòng trạng thái. Quét định kỳ cập nhật dòng trạng thái tại chỗ, không nối thêm nhật ký. Nếu nút Đóng chưa khớp, tiếp tục quét lại trong ngân sách 180 phút tính từ lúc bắt đầu xuất; hết thời gian hoặc bấm Dừng mới dừng vòng chờ. Video đã ghi xong vẫn giữ trạng thái Hoàn tất, project tiếp theo chưa chạy. Bản sửa cần kiểm tra trên PC thực tế.

## Chờ xuất video dài

Từ khi bắt đầu xuất video, giới hạn chờ mặc định và tối đa là **180 phút**. Cứ đến mỗi mốc 5 giây, ứng dụng tìm ảnh nút Đóng hoặc cặp nút Chia sẻ và Đóng và kiểm tra file MP4 tương ứng. Lần quét cuối ở phút 180 (giây 10.800) vẫn được nhận thành công nếu đủ điều kiện.

Quá trình quét định kỳ không ghi thêm log lên giao diện. Nhật ký chỉ ghi các bước thao tác, hoàn tất, dừng hoặc lỗi. Nút Dừng và góc dừng khẩn cấp vẫn hoạt động giữa các lần quét. Hết hạn chưa xác nhận được video thì dừng hàng đợi, giữ CapCut mở và không chuyển project tiếp theo.

Cấu hình 720 phút từ phiên bản trước được hạ xuống 180 phút khi mở bản mới, vẫn giữ profile và trạng thái project. Có thể nhập thời gian ngắn hơn, từ 1 đến 180 phút, trong ô Chờ xuất.

## Khi cần tự đặt cấu hình xuất

Bỏ chọn **Quy trình gọn** để hiện độ phân giải/FPS và các ảnh bổ sung. Chế độ này đặt tên, thư mục, MP4, độ phân giải và FPS, đọc lại đường dẫn trước khi xuất. Dùng hộp chọn thư mục chuẩn Windows, hoặc bỏ chọn hộp Windows để nhập full path nếu CapCut cho phép. Hộp chọn tùy biến Qt chưa được hỗ trợ. Xuất xong vẫn đóng/mở lại CapCut.

## Dừng và tiếp tục

Nút **Dừng** hoặc đưa chuột về góc trên trái hủy các thao tác tiếp theo, không hủy render đang chạy. Không dùng chuột/bàn phím hay khóa desktop trong khi xuất tự động.

Cấu hình, ảnh, trạng thái và nhật ký ở `%LOCALAPPDATA%\RenderBatchTemplates`. Có thể dùng lại 7 ảnh tương ứng đã học từ bản 1.0 nếu bố cục/DPI giữ nguyên; không cần xóa profile cũ. Job bị gián đoạn được đánh dấu **Cần kiểm tra**; video hoàn tất vẫn được giữ nếu khởi động lại CapCut lỗi. Job hoàn tất được bỏ qua khi tiếp tục cùng danh sách/cấu hình.

## Phát triển

Cài .NET 10 SDK để chạy source bằng `start.bat`, hoặc:

```powershell
dotnet test tests/RenderBatch.Tests/RenderBatch.Tests.csproj -c Release
dotnet build src/RenderBatch.Windows/RenderBatch.Windows.csproj -c Release
dotnet publish src/RenderBatch.Windows/RenderBatch.Windows.csproj -c Release -r win-x64 --self-contained true -o artifacts/windows
```

Chạy test và build tuần tự do dùng chung output core. Linux chạy được kiểm thử OpenCV native; WPF chỉ chạy trên Windows. Gói Windows loại plugin `opencv_videoio_ffmpeg*.dll` không dùng đến, giữ OpenCV nhận diện ảnh và .NET runtime. CapCut thực hiện render video. Cấu hình lựa chọn repository của môi trường được giữ nguyên.
