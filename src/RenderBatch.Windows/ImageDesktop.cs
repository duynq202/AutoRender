using System.Diagnostics;
using System.IO;
using RenderBatch.Core;
namespace RenderBatch.Windows;
public sealed class ImageDesktop(TemplateProfile profile,string directory,int timeoutSeconds,Action<string> log) : IDesktop
{
    private byte[]? lastFrame;
    private async Task<(WindowRef Window,TemplateHit Hit)?> Try(string role,CancellationToken token,bool tight=false)
    {
        var asset=profile.Require(role);token.ThrowIfCancellationRequested();var window=NativeDesktop.Find(asset.Scene);if(window==null)return null;NativeDesktop.Guard(token);if(!NativeDesktop.IsForegroundFor(window))return null;
        var png=NativeDesktop.Png(NativeDesktop.Capture(window,token));lastFrame=png;
        var hit=TemplateMatcher.Find(png,File.ReadAllBytes(Path.Combine(directory,asset.File)),asset,window.Dpi,profile.Threshold,tight,asset.Scene==Scene.Success);
        await Task.CompletedTask;return hit==null?null:(window,hit);
    }
    private async Task<(WindowRef Window,TemplateHit Hit)> Locate(string role,CancellationToken token,bool tight=false)
    {
        var timer=Stopwatch.StartNew();while(timer.Elapsed<TimeSpan.FromSeconds(timeoutSeconds))
        {var hit=await Try(role,token,tight);if(hit!=null)return hit.Value;await Task.Delay(500,token);}
        if(lastFrame!=null){var folder=Path.Combine(directory,"diagnostics");Directory.CreateDirectory(folder);var path=Path.Combine(folder,DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+role+".png");File.WriteAllBytes(path,lastFrame);log("Đã lưu ảnh lỗi: "+path);}
        throw new TimeoutException("Không thấy ảnh mẫu: "+role);
    }
    public async Task Wait(string role,CancellationToken token)=>_ = await Locate(role,token);
    public async Task Preview(string role,CancellationToken token)
    {var(w,h)=await Locate(role,token);NativeDesktop.Point(w.Bounds.X+h.X,w.Bounds.Y+h.Y,token);log($"{role}: khớp {h.Score:F3}, điểm ({h.X:F0},{h.Y:F0}) trong cửa sổ; chưa bấm.");}
    public async Task Click(string role,CancellationToken token)
    {
        var(w,h)=await Locate(role,token);NativeDesktop.Point(w.Bounds.X+h.X,w.Bounds.Y+h.Y,token);await Task.Delay(300,token);
        var(w2,h2)=await Locate(role,token);
        if(w2.Handle!=w.Handle||w2.Bounds!=w.Bounds||Math.Abs(h2.X-h.X)>3||Math.Abs(h2.Y-h.Y)>3)throw new InvalidOperationException("Vị trí nút thay đổi. Không bấm.");
        NativeDesktop.Click(w.Bounds.X+h.X,w.Bounds.Y+h.Y,token);
        await Task.Delay(250,token);log("Đã bấm ảnh: "+role);
    }
    public async Task Search(string name,CancellationToken token)
    {
        if(await Try("search-field",token,true)==null)await Click("search-toggle",token);
        await Fill("search-field",name,token);await Task.Delay(1000,token);
    }
    public Task<string> ExportTitle(CancellationToken token)
    {NativeDesktop.Guard(token);return Task.FromResult(NativeDesktop.Find(Scene.Export)?.Title??"");}
    public async Task Fill(string role,string value,CancellationToken token){await Click(role,token);await NativeDesktop.Fill(value,token);}
    public async Task<string> Read(string role,CancellationToken token){await Click(role,token);return await NativeDesktop.Copy(token);}
    public async Task ChooseFolder(string folder,CancellationToken token){await Click("output-picker",token);await NativeDesktop.Folder(folder,token);}
    public async Task Select(string menu,string value,CancellationToken token)
    {
        if(await Try(value,token,true)!=null)return;
        await Click(menu,token);await Click(value,token);await Locate(value,token,true);
    }
    public async Task<bool> Success(CancellationToken token)=>await Try("success",token,true)!=null;
    public async Task Home(CancellationToken token){NativeDesktop.CloseEditor(token);await Task.Delay(600,token);await Wait("home",token);}
}
