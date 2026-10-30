using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RenderBatch.Core;
using Validation=RenderBatch.Core.Validation;
namespace RenderBatch.Windows;
public record RoleChoice(string Id,string Label);
public partial class MainWindow : Window
{
 private readonly string data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RenderBatchTemplates");
 private TemplateProfile profile=new();private Session? session;private CancellationTokenSource? cancellation;private bool busy,closeWhenStopped;
 private static readonly JsonSerializerOptions Json=new(){WriteIndented=true};
 public MainWindow()
 {
  InitializeComponent();Directory.CreateDirectory(Path.Combine(data,"assets"));
  try{var p=Path.Combine(data,"profile.json");if(File.Exists(p))profile=JsonSerializer.Deserialize<TemplateProfile>(File.ReadAllText(p))??new();
   var s=Path.Combine(data,"session.json");if(File.Exists(s)){session=JsonSerializer.Deserialize<Session>(File.ReadAllText(s));if(session!=null){session=session with{Settings=session.Settings with{ExportMinutes=Math.Clamp(session.Settings.ExportMinutes,1,180)}};for(var i=0;i<session.Jobs.Count;i++)if(session.Jobs[i].State==JobState.Running)session.Jobs[i]=session.Jobs[i] with{State=JobState.Review,Detail="Phiên trước bị gián đoạn; kiểm tra CapCut/file trước khi nhập lại danh sách."};
    Exe.Text=session.Settings.CapCutPath;Output.Text=session.Settings.OutputFolder;Timeout.Text=session.Settings.ExportMinutes.ToString();FolderPicker.IsChecked=session.Settings.UseFolderPicker;SimpleFlow.IsChecked=session.Settings.SimpleWorkflow;
    Resolution.SelectedIndex=Array.IndexOf(new[]{"720p","1080p","4k"},session.Settings.Resolution);Fps.SelectedIndex=session.Settings.Fps=="60"?1:0;Names.Text=string.Join(Environment.NewLine,session.Jobs.Select(j=>j.Name));SaveSession(session);}}
  }catch(Exception ex){Message(ex);}
  RefreshRoles();Jobs.ItemsSource=session?.Jobs;Write("Quy trình gọn: 7 ảnh; xuất xong đóng và mở lại CapCut. Góc trên trái là dừng khẩn cấp.");
 }
 private Settings Settings(bool strict=true)=>new(Exe.Text.Trim(),Output.Text.Trim(),((ComboBoxItem)Resolution.SelectedItem).Content.ToString()!,((ComboBoxItem)Fps.SelectedItem).Content.ToString()!,FolderPicker.IsChecked==true,ExportMinutes:int.TryParse(Timeout.Text,out var minutes)?minutes:strict?throw new FormatException("Thời gian chờ phải là số nguyên."):180,SimpleWorkflow:SimpleFlow.IsChecked==true);
 private void Atomic(string name,object value){var path=Path.Combine(data,name);File.WriteAllText(path+".tmp",JsonSerializer.Serialize(value,Json));File.Move(path+".tmp",path,true);}
 private void SaveSession(Session s){Atomic("session.json",s);Dispatcher.Invoke(()=>{Jobs.ItemsSource=s.Jobs.ToArray();});}
 private void Write(string text){if(!Dispatcher.CheckAccess()){Dispatcher.Invoke(()=>Write(text));return;}var line=$"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} {text}{Environment.NewLine}";Log.AppendText(line);Log.ScrollToEnd();try{File.AppendAllText(Path.Combine(data,"activity.log"),line);}catch(IOException){}}
 private void Message(Exception ex){Write(ex.ToString());MessageBox.Show(this,ex.GetBaseException().Message,"RenderBatch",MessageBoxButton.OK,MessageBoxImage.Warning);}
 private void RefreshRoles(){if(Role==null||SimpleFlow==null||Resolution.SelectedItem==null||Fps.SelectedItem==null)return;AdvancedOptions.Visibility=SimpleFlow.IsChecked==true?Visibility.Collapsed:Visibility.Visible;var selected=Role.SelectedValue as string;Role.ItemsSource=Roles.For(Settings(false)).Keys.Select(id=>new RoleChoice(id,Roles.Description(id))).ToArray();Role.SelectedValue=selected;if(Role.SelectedItem==null)Role.SelectedIndex=0;var required=Roles.Required(Settings(false)).ToArray();AssetsStatus.Text=$"Đã học {required.Count(r=>profile.Assets.ContainsKey(r))}/{required.Length} ảnh cần dùng. Profile: {data}";}
 private void ModeChanged(object sender,RoutedEventArgs e)=>RefreshRoles();
 private void OptionsChanged(object sender,SelectionChangedEventArgs e){if(IsLoaded)RefreshRoles();}
 private void Exe_Click(object sender,RoutedEventArgs e){var d=new OpenFileDialog{Filter="CapCut.exe|CapCut.exe"};if(d.ShowDialog()==true)Exe.Text=d.FileName;}
 private void Folder_Click(object sender,RoutedEventArgs e){var d=new OpenFolderDialog();if(d.ShowDialog()==true)Output.Text=d.FolderName;}
 private void Import_Click(object sender,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Danh sách UTF-8|*.txt"};if(d.ShowDialog()==true)try{Names.Text=File.ReadAllText(d.FileName,new UTF8Encoding(false,true));}catch(Exception ex){Message(ex);}}
 private void SetBusy(bool value){busy=value;Configuration.IsEnabled=Names.IsEnabled=Start.IsEnabled=!value;Stop.IsEnabled=value;}
 private async Task Work(Func<CancellationToken,Task> action)
 {if(busy)return;using var source=new CancellationTokenSource();cancellation=source;SetBusy(true);try{await Task.Run(()=>action(source.Token));Status.Text="Đã hoàn tất thao tác";}catch(OperationCanceledException){Status.Text="Đã dừng; kiểm tra render trong CapCut";Write(Status.Text);}catch(Exception ex){Status.Text="Đã dừng do lỗi";Write(ex.ToString());Message(ex);}finally{cancellation=null;SetBusy(false);if(closeWhenStopped)Close();}}
 private async void Teach_Click(object sender,RoutedEventArgs e)
 {
  try
  {
   var role=Role.SelectedValue as string??throw new InvalidOperationException("Chọn vai trò ảnh.");var scene=Roles.For(Settings())[role];
   var executable=Exe.Text;
   System.Windows.Media.Imaging.BitmapSource? image=null;WindowRef? window=null;
   await Work(async token=>{await NativeDesktop.Connect(executable,token);window=(scene==Scene.Success?NativeDesktop.FindForegroundCapCut():NativeDesktop.Find(scene))??throw new InvalidOperationException($"Hãy mở màn hình {scene} trong CapCut rồi học ảnh.");NativeDesktop.Focus(window);NativeDesktop.Point(window.Bounds.X+window.Bounds.Width/2,window.Bounds.Y+8,token);await Task.Delay(350,token);image=NativeDesktop.Capture(window,token);});
   if(image==null||window==null)return;
   var teach=new TeachWindow(image,role){Owner=this};if(teach.ShowDialog()!=true||teach.Region==null||teach.LearnedImage==null)return;
   var file="assets/"+role+".png";var png=NativeDesktop.Png(teach.LearnedImage);
   var asset=new Asset(role,scene,file,image.PixelWidth,image.PixelHeight,window.Dpi,teach.Region,teach.TargetX-teach.Region.X,teach.TargetY-teach.Region.Y,
    role.StartsWith("resolution-")||role.StartsWith("fps-")||role=="format-mp4"?256:64);
   if(TemplateMatcher.Find(NativeDesktop.Png(image),png,asset,window.Dpi,profile.Threshold)==null)throw new InvalidOperationException("Ảnh mới không khớp ảnh gốc; chưa lưu.");
   File.WriteAllBytes(Path.Combine(data,file)+".tmp",png);File.Move(Path.Combine(data,file)+".tmp",Path.Combine(data,file),true);
   profile.Assets[role]=asset;Atomic("profile.json",profile);RefreshRoles();Write("Đã học ảnh và điểm bấm: "+role);
  }catch(Exception ex){Message(ex);}
 }
 private async void Preview_Click(object sender,RoutedEventArgs e)
 {try{var role=Role.SelectedValue as string??throw new InvalidOperationException("Chọn ảnh.");profile.Require(role);var settings=Settings();var desktop=new ImageDesktop(profile,data,settings.OpenSeconds,Write);await Work(async token=>{await NativeDesktop.Connect(settings.CapCutPath,token);await desktop.Preview(role,token);});}catch(Exception ex){Message(ex);}}
 private async void Start_Click(object sender,RoutedEventArgs e)
 {
  try
  {
   var settings=Settings();Validation.Settings(settings);
   if(!File.Exists(settings.CapCutPath)||!Path.GetFileName(settings.CapCutPath).Equals("CapCut.exe",StringComparison.OrdinalIgnoreCase))throw new FileNotFoundException("Chọn CapCut.exe hợp lệ để mở lại sau mỗi video.");
   var names=Validation.Names(Names.Text);profile.Validate(Roles.Required(settings));
   if(session!=null&&(session.Settings!=settings||!session.Jobs.Select(j=>j.Name).SequenceEqual(names)||session.Jobs.Any(j=>j.State==JobState.Review)))
   {if(MessageBox.Show(this,"Tạo lại trạng thái hàng đợi? Hãy kiểm tra CapCut không còn render trước. Video đã có không bị xóa hoặc ghi đè.","Hàng đợi",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;session=null;}
   session??=new(settings,names.Select(n=>new Job(n)).ToList());SaveSession(session);
   var desktop=new ImageDesktop(profile,data,settings.OpenSeconds,Write);var runner=new QueueRunner(desktop,SaveSession,Write,report:text=>Dispatcher.Invoke(()=>Status.Text=text));
   await Work(async token=>{await NativeDesktop.Connect(settings.CapCutPath,token);await runner.Run(session,token);});
  }catch(Exception ex){Message(ex);}
 }
 private void Stop_Click(object sender,RoutedEventArgs e)=>cancellation?.Cancel();
 private void ClosingWindow(object? sender,CancelEventArgs e){if(busy){e.Cancel=true;closeWhenStopped=true;cancellation?.Cancel();}}
}
