using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RenderBatch.Core;

namespace RenderBatch.Windows;
public record WindowRef(IntPtr Handle, int Pid, string Title, Rect Bounds, uint Dpi);
public static class NativeDesktop
{
    private delegate bool EnumCallback(IntPtr handle, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr h,uint message,nuint wParam,nint lParam);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr handle,uint command);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr h);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x,int y);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out PointI p);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern int GetClassName(IntPtr h,StringBuilder s,int max);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr h);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr h,IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr dc,int w,int h);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr dc,int x,int y,int w,int h,IntPtr src,int sx,int sy,uint op);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern int GetDIBits(IntPtr dc,IntPtr bmp,uint first,uint lines,byte[] bytes,ref BitmapInfo info,uint usage);
    [StructLayout(LayoutKind.Sequential)] private struct PointI { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo { public uint Size;public int Width,Height;public ushort Planes,Bits;public uint Compression,SizeImage;public int XPels,YPels;public uint Used,Important; }
    [StructLayout(LayoutKind.Sequential)] private struct Mouse { public int X,Y;public uint Data,Flags,Time;public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] private struct Key { public ushort Code,Scan;public uint Flags,Time;public nuint Extra; }
    [StructLayout(LayoutKind.Explicit)] private struct Union { [FieldOffset(0)] public Mouse Mouse;[FieldOffset(0)] public Key Key; }
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type;public Union Data; }
    [DllImport("user32.dll")] private static extern uint SendInput(uint count,Input[] inputs,int size);

    private static List<(IntPtr Handle,int Pid)> Handles()
    {
        var ids=new HashSet<int>();foreach(var p in Process.GetProcessesByName("CapCut")){ ids.Add(p.Id);p.Dispose(); }
        var windows=new List<(IntPtr,int)>();
        EnumWindows((h,_)=> { GetWindowThreadProcessId(h,out var pid);if(ids.Contains((int)pid)&&IsWindowVisible(h))windows.Add((h,(int)pid));return true; },IntPtr.Zero);
        return windows;
    }
    public static WindowRef? Find(Scene scene)
    {
        var id=scene switch { Scene.Home=>"HomeWindow",Scene.Editor=>"MainWindow",_=>"ExportWindow_Container" };
        var matches=new List<WindowRef>();
        foreach(var (handle,pid) in Handles())
        {
            try { var c=AutomationElement.FromHandle(handle).Current;
                if(c.AutomationId==id&&!c.BoundingRectangle.IsEmpty&&c.BoundingRectangle.Width>320&&c.BoundingRectangle.Height>200)
                    matches.Add(new(handle,pid,c.Name,c.BoundingRectangle,GetDpiForWindow(handle)));
            }catch(ElementNotAvailableException){}
        }
        if(matches.Count>1)throw new InvalidOperationException("Có nhiều cửa sổ CapCut trùng vai trò. Chỉ giữ một phiên.");
        return matches.SingleOrDefault();
    }
    public static void CheckStop(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if(GetCursorPos(out var p)&&p.X<=1&&p.Y<=1)throw new OperationCanceledException("Dừng khẩn cấp.",token);
    }
    public static void Guard(CancellationToken token)
    {
        CheckStop(token);
        GetWindowThreadProcessId(GetForegroundWindow(),out var pid);
        using var process=Process.GetProcessById((int)pid);
        if(!process.ProcessName.Equals("CapCut",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Focus rời CapCut; đã dừng thao tác.");
    }
    public static void Focus(WindowRef w)
    {
        if(GetForegroundWindow()!=w.Handle&&!SetForegroundWindow(w.Handle))throw new InvalidOperationException("Không kích hoạt được cửa sổ CapCut.");
    }
    public static bool IsForegroundFor(WindowRef w)
    {
        var foreground=GetForegroundWindow();
        for(var depth=0;depth<8&&foreground!=IntPtr.Zero;depth++){if(foreground==w.Handle)return true;foreground=GetWindow(foreground,4);}
        return false;
    }
    public static async Task Connect(string executable,CancellationToken token)
    {
        if(Handles().Count==0)
        {
            if(!File.Exists(executable)||!Path.GetFileName(executable).Equals("CapCut.exe",StringComparison.OrdinalIgnoreCase))throw new FileNotFoundException("Chọn CapCut.exe.");
            Process.Start(new ProcessStartInfo(executable){UseShellExecute=true});
        }
        var timer=Stopwatch.StartNew();
        while(timer.Elapsed<TimeSpan.FromSeconds(90))
        {
            token.ThrowIfCancellationRequested();var w=Find(Scene.Export)??Find(Scene.Home)??Find(Scene.Editor);
            if(w!=null){using var p=Process.GetProcessById(w.Pid);var version=FileVersionInfo.GetVersionInfo(p.MainModule!.FileName).ProductVersion??"";
                if(!RenderBatch.Core.Validation.CapCutVersion(version))throw new InvalidOperationException("Bản này cho phép CapCut 9.6.0 hoặc 9.7.0; phiên bản hiện tại: "+version);
                Focus(w);await Task.Delay(300,token);return;}
            await Task.Delay(500,token);
        }
        throw new TimeoutException("Không nhận diện được cửa sổ CapCut.");
    }
    public static BitmapSource Capture(WindowRef w,CancellationToken token)
    {
        Guard(token);
        var desktop=new Rect(GetSystemMetrics(76),GetSystemMetrics(77),GetSystemMetrics(78),GetSystemMetrics(79));
        if(!desktop.Contains(w.Bounds))throw new InvalidOperationException("Đưa toàn bộ cửa sổ CapCut vào màn hình trước khi học/tìm ảnh.");
        if(!IsForegroundFor(w))throw new InvalidOperationException("CapCut đang ở màn hình khác hoặc cửa sổ bị che. Mở đúng màn hình cần tìm ảnh.");
        var width=(int)Math.Round(w.Bounds.Width);var height=(int)Math.Round(w.Bounds.Height);
        var screen=GetDC(IntPtr.Zero);var dc=CreateCompatibleDC(screen);var bitmap=CreateCompatibleBitmap(screen,width,height);var old=SelectObject(dc,bitmap);
        try
        {
            if(!BitBlt(dc,0,0,width,height,screen,(int)Math.Round(w.Bounds.X),(int)Math.Round(w.Bounds.Y),0x40CC0020))throw new IOException("Không chụp được CapCut.");
            SelectObject(dc,old);var pixels=new byte[width*height*4];var info=new BitmapInfo{Size=(uint)Marshal.SizeOf<BitmapInfo>(),Width=width,Height=-height,Planes=1,Bits=32};
            if(GetDIBits(screen,bitmap,0,(uint)height,pixels,ref info,0)!=height)throw new IOException("Không đọc được ảnh.");
            var source=BitmapSource.Create(width,height,96,96,PixelFormats.Bgr32,null,pixels,width*4);source.Freeze();return source;
        }
        finally{SelectObject(dc,old);DeleteObject(bitmap);DeleteDC(dc);ReleaseDC(IntPtr.Zero,screen);}
    }
    public static byte[] Png(BitmapSource image)
    {var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var output=new MemoryStream();encoder.Save(output);return output.ToArray();}
    private static Input K(ushort key,bool up=false)=>new(){Type=1,Data=new(){Key=new(){Code=key,Flags=up?2u:0}}};
    private static void Send(Input[] inputs){if(SendInput((uint)inputs.Length,inputs,Marshal.SizeOf<Input>())!=inputs.Length)throw new InvalidOperationException("Windows từ chối thao tác chuột/phím. Kiểm tra mức quyền ứng dụng.");}
    public static void Ctrl(ushort key,CancellationToken token){Guard(token);Send([K(0x11),K(key),K(key,true),K(0x11,true)]);}
    public static void Enter(CancellationToken token){Guard(token);Send([K(0x0D),K(0x0D,true)]);}
    public static void Text(string text,CancellationToken token)
    {Guard(token);var events=new List<Input>();foreach(var c in text){events.Add(new(){Type=1,Data=new(){Key=new(){Scan=c,Flags=4}}});events.Add(new(){Type=1,Data=new(){Key=new(){Scan=c,Flags=6}}});}Send(events.ToArray());}
    public static void Point(double x,double y,CancellationToken token){Guard(token);if(!SetCursorPos((int)Math.Round(x),(int)Math.Round(y)))throw new IOException("Không di chuyển được chuột.");}
    public static void Click(double x,double y,CancellationToken token){Point(x,y,token);Send([new(){Data=new(){Mouse=new(){Flags=2}}},new(){Data=new(){Mouse=new(){Flags=4}}}]);}
    public static async Task<string> Copy(CancellationToken token)
    {
        Application.Current.Dispatcher.Invoke(()=>Clipboard.SetText("RenderBatch-"+Guid.NewGuid().ToString("N")));
        Ctrl(0x41,token);Ctrl(0x43,token);await Task.Delay(250,token);
        return Application.Current.Dispatcher.Invoke(()=>Clipboard.GetText());
    }
    public static async Task Fill(string text,CancellationToken token)
    {Ctrl(0x41,token);Text(text,token);if(!string.Equals(await Copy(token),text,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Giá trị ô nhập đọc lại không đúng. Đã dừng.");}
    public static async Task Folder(string folder,CancellationToken token)
    {
        AutomationElement? dialog=null;var timer=Stopwatch.StartNew();
        while(timer.Elapsed<TimeSpan.FromSeconds(15))
        {
            Guard(token);
            foreach(var(h,_)in Handles()){var cls=new StringBuilder(256);GetClassName(h,cls,256);if(cls.ToString()=="#32770"){dialog=AutomationElement.FromHandle(h);break;}}
            if(dialog!=null)break;await Task.Delay(200,token);
        }
        if(dialog==null)throw new NotSupportedException("Chưa hỗ trợ hộp chọn thư mục này. Chọn chế độ nhập full path nếu ô Xuất sang cho nhập.");
        Ctrl(0x4C,token);await Fill(folder,token);Enter(token);await Task.Delay(1200,token);
        var buttons=dialog.FindAll(TreeScope.Descendants,new AndCondition(new PropertyCondition(AutomationElement.AutomationIdProperty,"1"),new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Button)))
            .Cast<AutomationElement>().Where(e=>e.Current.IsEnabled&&!e.Current.IsOffscreen).ToArray();
        if(buttons.Length!=1||!buttons[0].TryGetCurrentPattern(InvokePattern.Pattern,out var action))throw new NotSupportedException("Không xác định được nút chọn thư mục của Windows.");
        Guard(token);((InvokePattern)action).Invoke();await Task.Delay(500,token);
    }
    // Called only after the queue has positively verified the completed output.
    public static async Task Restart(string executable,CancellationToken token)
    {
        CheckStop(token);
        if(!File.Exists(executable)||!Path.GetFileName(executable).Equals("CapCut.exe",StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("Chọn CapCut.exe hợp lệ để mở lại sau khi xuất.");
        var timer=Stopwatch.StartNew();var requested=new HashSet<IntPtr>();
        while(timer.Elapsed<TimeSpan.FromSeconds(45))
        {
            CheckStop(token);
            var processes=Process.GetProcessesByName("CapCut");
            try{if(processes.Length==0)break;}finally{foreach(var p in processes)p.Dispose();}
            var w=Find(Scene.Success)??Find(Scene.Editor)??Find(Scene.Home);
            if(w!=null&&requested.Add(w.Handle))
            {
                Focus(w);await Task.Delay(200,token);Guard(token);
                // WM_CLOSE is asynchronous: a save/error dialog must not block cancellation.
                if(!PostMessage(w.Handle,0x0010,0,0))throw new InvalidOperationException("Không gửi được yêu cầu đóng CapCut.");
            }
            await Task.Delay(500,token);
        }
        var remaining=Process.GetProcessesByName("CapCut");
        try{if(remaining.Length!=0)throw new TimeoutException("CapCut chưa thoát sau 45 giây. Kiểm tra hộp thoại đang mở; chưa chạy project tiếp theo.");}
        finally{foreach(var p in remaining)p.Dispose();}
        CheckStop(token);
        Process.Start(new ProcessStartInfo(executable){UseShellExecute=true});
        await Connect(executable,token);
    }
}
