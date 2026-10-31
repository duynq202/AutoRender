# RenderBatch 1.1.12 — xuất CapCut theo vòng lặp 7 ảnh

C#/.NET 10 + WPF + OpenCvSharp. Tham khảo cơ chế nhận diện ảnh của RenderCapcutGUI.exe; [báo cáo phân tích](docs/REFERENCE-ANALYSIS.md). Không sử dụng OCR.

**Trạng thái:** đã kiểm thử lõi và build gói Windows. Cần học ảnh trên PC của bạn và thử một project ngắn; chưa kiểm chứng toàn bộ luồng trên Windows có CapCut.

## CapCut 9.7.0

Bản 1.1.12 cho phép kết nối CapCut 9.7.0 bên cạnh 9.6.0, gồm các chuỗi phiên bản có hậu tố build như `9.7.0.123`. Các phiên bản khác vẫn bị chặn.

Đây là cập nhật điều kiện nhận phiên bản; chưa kiểm chứng thao tác GUI trên CapCut 9.7.0 thực tế. Khi nâng từ 9.6 lên 9.7, học lại các ảnh có giao diện thay đổi, kiểm tra điểm bấm bằng chế độ chỉ trỏ chuột và thử một project ngắn. Giữ nguyên profile cũ để có thể dùng lại, không tự chuyển đổi ảnh cũ thành ảnh 9.7.0. Nếu cửa sổ/nhãn thay đổi và không nhận diện được, ứng dụng dừng để kiểm tra.

## Quy trình mặc định

Trang chủ CapCut → icon tìm kiếm → nhập tên ngay → mở project → thử nút Xuất mỗi 5 giây, tối đa 120 giây → bảng xuất → nút bắt đầu xuất → ảnh nút Đóng khớp hai lần cách 1 giây → kết thúc tiến trình CapCut → mở lại, chờ trang chủ → project tiếp theo.

Giữ nguyên **7 ảnh** đã học từ 1.1.8–1.1.10; không cần ảnh ô nhập tìm kiếm. Icon tìm kiếm tự đặt focus vào ô nhập; ứng dụng gõ tên và đọc lại để kiểm tra. Ở bước chờ xuất, ảnh **nút Đóng riêng** là tín hiệu hoàn tất chính. Khi ảnh khớp hai lần cách nhau 1 giây trên cùng cửa sổ/tiến trình và vị trí ổn định, ứng dụng kết thúc cây tiến trình CapCut đã xác nhận, chờ thoát tối đa 15 giây rồi mở lại và chờ trang chủ. Sau project cuối cũng mở lại trang chủ nếu không chọn tắt máy. Nếu chưa thoát được, hàng đợi dừng và chưa chạy project tiếp theo.

**MP4 chỉ dùng kiểm tra bổ sung:** chưa tìm thấy file đúng tên, file rỗng hoặc bị khóa sẽ ghi một cảnh báo và tiếp tục đóng/mở lại theo ảnh hoàn tất. Trạng thái Hoàn tất lúc này xác nhận theo giao diện CapCut, không chứng minh file video đã được kiểm tra thành công. Hãy học đúng nút Đóng ở bảng sau khi xuất xong.

## Chạy lần đầu

1. Giải nén **toàn bộ** gói Windows x64, mở `RenderBatch.Windows.exe`. Không cần cài Python hoặc .NET SDK. Cho phép kết nối CapCut 9.6.0 / 9.7.0, giao diện tiếng Việt và desktop mở khóa.
2. Chọn đường dẫn `CapCut.exe` để có thể mở lại sau mỗi video.
3. Trong CapCut, đặt sẵn **thư mục lưu, MP4, độ phân giải, FPS** (ví dụ 1080p/30fps). Đặt tên file trùng project để kiểm tra MP4 bổ sung được. Chuẩn bị canvas 16:9 từ trước. Chạy thử thủ công để xác nhận CapCut giữ thiết lập sau khi đóng/mở lại.
4. Trong RenderBatch chọn **cùng thư mục video**. Ở chế độ gọn, đây là thư mục kiểm tra bổ sung và chặn ghi đè file đã tồn tại; ứng dụng dùng các thiết lập hiện tại của CapCut, không tự đặt lại thư mục, tên hay thông số xuất.
5. Mở danh sách project ở **chế độ danh sách**, để ô tìm kiếm đang đóng trước khi bắt đầu hàng đợi. Học 7 ảnh theo [hướng dẫn](docs/TEACHING.md), sau đó dùng **Tìm ảnh, chỉ trỏ chuột** để kiểm tra vị trí, nhất là hàng kết quả đầu tiên.
6. Nhập file `.txt` UTF-8, mỗi dòng một tên project chính xác; bấm **Chạy / Tiếp tục**. Thử một project ngắn trước khi chạy danh sách dài.

## Xác nhận trước khi sang project tiếp theo

Tên tìm kiếm được gõ Unicode và đọc lại ngay sau khi bấm icon. Sau lọc, ứng dụng mở hàng đầu tiên, rồi thử mở bảng xuất ngay, mỗi 5 giây một lần trong tối đa 120 giây. Khi bảng đã xuất hiện thì dừng bấm nút Xuất trong editor. Trước khi bắt đầu render, tiêu đề bảng xuất phải khớp chính xác tên project. Nếu tên gần giống làm mở nhầm project, hàng đợi dừng trước khi render; chưa tự duyệt các hàng còn lại.

Hoàn tất dựa trên ảnh **nút Đóng riêng**, khớp trên hai ảnh chụp mới cách nhau 1 giây. Cả hai ảnh phải thuộc cùng cửa sổ, PID và thời điểm khởi động tiến trình; vị trí điểm nhận diện chênh không quá 3 pixel. Nếu ảnh biến mất hoặc thay đổi, tiếp tục quét ở mốc 5 giây kế tiếp, không kết thúc CapCut.

Bước này chụp cửa sổ CapCut đang ở phía trước bằng Win32, tìm ảnh trong toàn bộ phần cửa sổ hiển thị, không cần AutomationId hoặc tên của bảng chia sẻ. Vì thế vẫn hoạt động khi bảng được nhúng vào editor hoặc đổi vị trí. Chỉ xét cửa sổ thuộc tiến trình CapCut, cùng DPI lúc học; ảnh trùng nhiều nơi bị từ chối. Giữ CapCut ở phía trước và desktop mở khóa. Ngưỡng 0.97 là hệ số khớp ảnh, không phải xác suất đúng.

File `<tên project>.mp4` mở độc quyền được và có dữ liệu thì xác nhận kiểm tra bổ sung đạt. Nếu tên có hậu tố, khác thư mục, chưa có dữ liệu hoặc còn bị khóa, chỉ ghi cảnh báo, không chặn đóng CapCut. File đã tồn tại trước khi chạy vẫn bị chặn để tránh ghi đè; chế độ gọn không tự đặt hoặc đọc lại đường dẫn trong bảng xuất.

## Mở bảng xuất: mỗi 5 giây, tối đa 120 giây

Ngay sau khi mở project, ứng dụng thử nhận diện và bấm nút Xuất trong editor tại các mốc 0, 5, 10…115 giây, chỉ bấm khi ảnh và vị trí đã được xác minh ổn định. Giữa các lần thử, kiểm tra cửa sổ bảng xuất khoảng mỗi 0,5 giây. Dừng bấm ngay khi nhận diện cửa sổ này, kể cả lúc các trường cấu hình còn đang tải. Sau đó mới chờ nội dung bảng sẵn sàng, xác minh tên và xử lý cấu hình xuất.

Mỗi lần thử đều kiểm tra bảng xuất trước khi gửi click, dời chuột khỏi nút để bỏ trạng thái hover, tìm ảnh mới rồi xác minh lại vị trí. Nút Xuất trong editor được tìm theo mép phải/mép trên của cửa sổ, cùng DPI lúc học, cho phép editor đổi kích thước. Nếu thao tác nhận diện mất lâu, bỏ qua mốc đã lỡ và chờ mốc tiếp theo, không bấm dồn.

Hết 120 giây vẫn chưa có bảng xuất thì dừng hàng đợi, giữ CapCut mở và lưu chẩn đoán ở `%LOCALAPPDATA%\RenderBatchTemplates\diagnostics`. Nút Dừng và góc dừng khẩn cấp vẫn hoạt động giữa các lần thử. Thời gian này thay cho khoảng chờ cố định 180 giây của bản trước.

Nếu ảnh cũ được học khi hover, học lại riêng **Nút Xuất trong editor**: chọn vùng nhỏ quanh chữ/biểu tượng của nút, ghi điểm bấm ở tâm. Giữ các ảnh khác. Nếu không nhận diện được, nhật ký sẽ nêu lý do như chưa thấy editor, sai DPI hoặc ảnh chưa đạt ngưỡng.

## Tìm nút Đóng mỗi 5 giây

Sau khi bắt đầu xuất video, cứ 5 giây ứng dụng tìm ảnh **nút Đóng riêng**. Khi tìm thấy, chụp mới sau 1 giây để xác nhận. Nếu hai lần khớp ổn định, ghi nhận hoàn tất theo ảnh và kết thúc tiến trình CapCut trực tiếp. Không bấm nút Đóng của bảng chia sẻ, không chờ file MP4 để cho phép đóng.

Nhật ký ghi các mốc **Đã thấy nút Đóng → Đang kết thúc CapCut (PID…) → CapCut đã thoát**, cùng cảnh báo MP4 nếu có. Không ghi log từng lần quét. Nếu Windows từ chối kết thúc tiến trình, ghi lỗi và giữ project tiếp theo chưa chạy; không mở thêm phiên mới.

**Nếu đã có đủ 7 ảnh ở 1.1.8, giữ nguyên profile, không cần học lại. Nâng từ 1.1.7 cần học thêm một ảnh**, giao diện sẽ hiện 6/7 nếu các ảnh trước đã đủ:

1. Giữ bảng sau khi xuất xong đang hiển thị (như ảnh bạn gửi).
2. Trong RenderBatch chọn **Hoàn tất: nút Đóng riêng** rồi bấm **Học ảnh / điểm bấm**.
3. Khoanh gọn **chỉ nút Đóng ở góc dưới bên phải**; sau đó bấm vào chính tâm nút Đóng trên ảnh và lưu. Tránh nút Chia sẻ hoặc nút X ở thanh tiêu đề.
4. Dùng **Tìm ảnh, chỉ trỏ chuột** để kiểm tra điểm. Về trang chủ CapCut trước khi chạy hàng đợi; thử với project chưa có file đầu ra.

Ảnh `completion-close` được tìm trên toàn cửa sổ CapCut đang hiển thị, cùng DPI lúc học. Cặp nút `completion-actions` vẫn được giữ trong profile và có thể kiểm tra bằng con trỏ, nhưng không thay thế nút Đóng riêng trong điều kiện tự động hoàn tất. Không chọn ảnh chỉ có nền phẳng hoặc chỉ một phần viền nút.

Nếu ảnh chưa khớp ổn định, tiếp tục quét trong ngân sách 180 phút tính từ lúc bắt đầu xuất. Có thể bấm Dừng hoặc đưa chuột về góc trên trái để hủy thao tác tiếp theo. Sau khi đã xác nhận ảnh, lỗi đóng/mở lại không làm mất trạng thái Hoàn tất theo ảnh của project hiện tại.

## Chờ xuất video dài

Từ khi bắt đầu xuất video, giới hạn chờ mặc định và tối đa là **180 phút**. Cứ đến mỗi mốc 5 giây, ứng dụng tìm ảnh nút Đóng. Lần quét cuối ở phút 180 (giây 10.800) vẫn được nhận thành công nếu xác nhận lại sau 1 giây đạt yêu cầu; bước xác nhận này có thể vượt mốc chờ thêm khoảng 1 giây cộng thời gian nhận diện.

Quá trình quét định kỳ không ghi thêm log lên giao diện. Nhật ký chỉ ghi các bước thao tác, hoàn tất, dừng hoặc lỗi. Nút Dừng và góc dừng khẩn cấp vẫn hoạt động giữa các lần quét. Hết hạn chưa xác nhận được video thì dừng hàng đợi, giữ CapCut mở và không chuyển project tiếp theo.

Cấu hình 720 phút từ phiên bản trước được hạ xuống 180 phút khi mở bản mới, vẫn giữ profile và trạng thái project. Có thể nhập thời gian ngắn hơn, từ 1 đến 180 phút, trong ô Chờ xuất.

## Khi cần tự đặt cấu hình xuất

Bỏ chọn **Quy trình gọn** để hiện độ phân giải/FPS và các ảnh bổ sung. Chế độ này đặt tên, thư mục, MP4, độ phân giải và FPS, đọc lại đường dẫn trước khi xuất. Dùng hộp chọn thư mục chuẩn Windows, hoặc bỏ chọn hộp Windows để nhập full path nếu CapCut cho phép. Hộp chọn tùy biến Qt chưa được hỗ trợ. Xuất xong vẫn đóng/mở lại CapCut.

## Tắt máy khi xuất xong toàn bộ danh sách

Đánh dấu **Tắt máy khi hoàn tất tất cả project** trước khi bấm Chạy / Tiếp tục. Mặc định chưa bật khi cài mới hoặc mở cấu hình cũ; lựa chọn được lưu cùng cấu hình. Đổi riêng lựa chọn này không đặt lại trạng thái các project đã hoàn tất.

Khi tất cả project trong danh sách hoàn tất và đóng CapCut thành công, RenderBatch đếm ngược **30 giây** trong dòng trạng thái. Sau project cuối không mở lại CapCut nếu đã chọn tắt máy. Bấm **Dừng**, đưa chuột về góc trên trái hoặc đóng RenderBatch trong lúc đếm ngược để hủy; trạng thái project vẫn giữ nguyên. Chưa có lịch tắt máy nào gửi sang Windows trong thời gian đếm ngược.

Chỉ gửi lệnh tắt máy khi lượt chạy này đã xử lý ít nhất một project và toàn bộ hàng đợi hoàn tất, không có lỗi hay hủy. Danh sách trống, danh sách đã hoàn tất từ trước, lỗi render/nhận diện, lỗi đóng CapCut hoặc bấm Dừng đều không tự tắt máy. Bấm chạy lại sau khi hủy đếm ngược không tự kích hoạt lại tắt máy với danh sách đã hoàn tất.

Hoàn tất vẫn theo điều kiện ảnh nút Đóng của phiên bản hiện tại; cảnh báo MP4 không chặn tắt máy. Lệnh tắt máy không ép đóng ứng dụng khác; Windows có thể yêu cầu xử lý ứng dụng đang có công việc chưa lưu. Khi đã gửi lệnh, Windows xử lý việc tắt máy; nút Dừng chỉ hủy trước thời điểm gửi.

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
