using System.Threading;
using System.Windows;
namespace AutoRender.App;
public partial class App : Application
{
    private Mutex? mutex;
    protected override void OnStartup(StartupEventArgs e)
    {
        mutex = new Mutex(true, @"Local\AutoRender.CapCut.Queue", out var created);
        if (!created) { MessageBox.Show("AutoRender đang chạy. Chỉ sử dụng một phiên."); mutex.Dispose(); mutex = null; Shutdown(); return; }
        base.OnStartup(e);
    }
    protected override void OnExit(ExitEventArgs e) { mutex?.ReleaseMutex(); mutex?.Dispose(); base.OnExit(e); }
}
