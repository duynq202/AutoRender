using System.Windows;
namespace RenderBatch.Windows;
public partial class App : Application
{
 private Mutex? mutex;
 protected override void OnStartup(StartupEventArgs e){mutex=new Mutex(true,@"Local\RenderBatch.Template",out var first);if(!first){MessageBox.Show("RenderBatch đang chạy.");mutex.Dispose();mutex=null;Shutdown();return;}base.OnStartup(e);}
 protected override void OnExit(ExitEventArgs e){mutex?.ReleaseMutex();mutex?.Dispose();base.OnExit(e);}
}
