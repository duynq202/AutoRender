using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RenderBatch.Core;
namespace RenderBatch.Windows;

public sealed class TeachWindow : Window
{
 private readonly Canvas canvas=new();private readonly TextBlock instruction=new(){Margin=new Thickness(10),TextWrapping=TextWrapping.Wrap};
 private readonly System.Windows.Shapes.Rectangle selection=new(){Stroke=Brushes.Lime,StrokeThickness=2,IsHitTestVisible=false};
 private readonly System.Windows.Shapes.Ellipse target=new(){Stroke=Brushes.OrangeRed,StrokeThickness=3,Width=14,Height=14,IsHitTestVisible=false};
 private readonly Button save=new(){Content="Lưu ảnh và điểm bấm",IsEnabled=false,Margin=new Thickness(10)};
 private readonly BitmapSource source;private readonly double factor;private System.Windows.Point? start;private bool chooseTarget;
 public Box? Region{get;private set;} public double TargetX{get;private set;} public double TargetY{get;private set;}
 public BitmapSource? LearnedImage{get;private set;}
 public TeachWindow(BitmapSource source,string role)
 {
  this.source=source;factor=Math.Min(1,1000.0/source.PixelWidth);Title="Học ảnh: "+role;Width=Math.Min(1060,source.PixelWidth+45);Height=850;
  var dock=new DockPanel();Content=dock;DockPanel.SetDock(instruction,Dock.Top);dock.Children.Add(instruction);DockPanel.SetDock(save,Dock.Bottom);dock.Children.Add(save);
  var reset=new Button{Content="Chọn lại vùng",Margin=new Thickness(10)};DockPanel.SetDock(reset,Dock.Bottom);dock.Children.Add(reset);
  var scroll=new ScrollViewer{HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};dock.Children.Add(scroll);
  canvas.Width=source.PixelWidth*factor;canvas.Height=source.PixelHeight*factor;canvas.Background=Brushes.Black;scroll.Content=canvas;
  canvas.Children.Add(new Image{Source=source,Width=canvas.Width,Height=canvas.Height,IsHitTestVisible=false});canvas.Children.Add(selection);canvas.Children.Add(target);target.Visibility=Visibility.Hidden;
  instruction.Text="1. Kéo vùng nhỏ quanh chữ/biểu tượng CỐ ĐỊNH để nhận diện; tránh tên project, số %, giá trị đang thay đổi.\n2. Sau khi thả chuột, bấm một lần vào ĐIỂM CẦN THAO TÁC trên ảnh (có thể nằm ngoài vùng đã chọn). Với first-result: chọn ảnh nhãn Tên và điểm ở hàng kết quả đầu tiên.";
  if(role=="completion-actions")instruction.Text="Khi CapCut đã xuất xong và hiện bảng chia sẻ: kéo vùng chứa CẢ HAI nút Chia sẻ và Đóng, tránh ảnh xem trước/phần trăm. Sau đó bấm tâm vùng trên ảnh để ghi dấu hiệu (ứng dụng chỉ nhận diện, không bấm nút Chia sẻ).";
  if(role=="completion-close")instruction.Text="Khi CapCut đã xuất xong: kéo vùng chỉ chứa nút ĐÓNG ở góc dưới bên phải bảng chia sẻ. Sau đó bấm chính TÂM NÚT ĐÓNG trên ảnh để kiểm tra vị trí. Hàng đợi xác nhận ảnh hai lần rồi kết thúc ứng dụng CapCut trực tiếp, không bấm nút này. Không chọn nút Chia sẻ hoặc nút X trên thanh tiêu đề.";
  canvas.MouseLeftButtonDown+=Down;canvas.MouseMove+=Move;canvas.MouseLeftButtonUp+=Up;
  save.Click+=(_,_)=>{if(Region==null)return;LearnedImage=new CroppedBitmap(source,new Int32Rect(Region.X,Region.Y,Region.Width,Region.Height));LearnedImage.Freeze();DialogResult=true;};
  reset.Click+=(_,_)=>{chooseTarget=false;Region=null;selection.Width=selection.Height=0;target.Visibility=Visibility.Hidden;save.IsEnabled=false;};
 }
 private System.Windows.Point Position(MouseEventArgs e){var p=e.GetPosition(canvas);return new(Math.Clamp(p.X,0,canvas.Width-1),Math.Clamp(p.Y,0,canvas.Height-1));}
 private void Down(object sender,MouseButtonEventArgs e)
 {
  var p=Position(e);
  if(chooseTarget){TargetX=p.X/factor;TargetY=p.Y/factor;Canvas.SetLeft(target,p.X-7);Canvas.SetTop(target,p.Y-7);target.Visibility=Visibility.Visible;save.IsEnabled=true;return;}
  start=p;canvas.CaptureMouse();Canvas.SetLeft(selection,p.X);Canvas.SetTop(selection,p.Y);selection.Width=selection.Height=0;
 }
 private void Move(object sender,MouseEventArgs e){if(start==null||chooseTarget)return;var p=Position(e);Canvas.SetLeft(selection,Math.Min(p.X,start.Value.X));Canvas.SetTop(selection,Math.Min(p.Y,start.Value.Y));selection.Width=Math.Abs(p.X-start.Value.X);selection.Height=Math.Abs(p.Y-start.Value.Y);}
 private void Up(object sender,MouseButtonEventArgs e)
 {
  if(start==null||chooseTarget)return;Move(sender,e);canvas.ReleaseMouseCapture();start=null;
  var x=(int)Math.Floor(Canvas.GetLeft(selection)/factor);var y=(int)Math.Floor(Canvas.GetTop(selection)/factor);
  var w=Math.Min(source.PixelWidth-x,(int)Math.Round(selection.Width/factor));var h=Math.Min(source.PixelHeight-y,(int)Math.Round(selection.Height/factor));
  if(w<8||h<8){MessageBox.Show(this,"Vùng ảnh tối thiểu 8 × 8 pixel.");return;}
  Region=new(x,y,w,h);chooseTarget=true;instruction.Text="Đã chọn ảnh. Bấm vào điểm cần thao tác trên ảnh, rồi Lưu. Ảnh dùng để tìm có thể là nhãn bên trái; điểm bấm có thể là ô nhập bên phải. Với marker chỉ để chờ, bấm tâm vùng ảnh.";
 }
}
