using System.Diagnostics;
using System.IO;
using RenderBatch.Core;
namespace RenderBatch.Windows;
public sealed class ImageDesktop(TemplateProfile profile,string directory,int timeoutSeconds,Action<string> log) : IDesktop
{
    private byte[]? lastFrame;
    private string lastMiss="Chưa có ảnh cửa sổ";
    private async Task<(WindowRef Window,TemplateHit Hit)?> Try(string role,CancellationToken token,bool tight=false)
    {
        var asset=profile.Require(role);NativeDesktop.CheckStop(token);
        var window=NativeDesktop.Find(asset.Scene);
        if(window==null){lastMiss="Chưa nhận diện được cửa sổ "+asset.Scene;return null;}
        var dimensions=$"hiện tại {window.Bounds.Width:F0}×{window.Bounds.Height:F0}, DPI {window.Dpi}; lúc học {asset.WindowWidth}×{asset.WindowHeight}, DPI {asset.Dpi}";
        // A heavy project can expose an editor that is still behind its loading window.
        if(role=="editor-export"&&!NativeDesktop.IsForegroundFor(window))
        {lastMiss="Editor chưa ở phía trước; "+dimensions;return null;}
        if(role=="editor-export"&&window.Dpi!=asset.Dpi)
        {lastMiss="DPI khác lúc học ảnh; cần học lại nút Xuất ở DPI hiện tại; "+dimensions;return null;}
        NativeDesktop.Guard(token);if(!NativeDesktop.IsForegroundFor(window)){lastMiss="Cửa sổ cần tìm chưa ở phía trước";return null;}
        var png=NativeDesktop.Png(NativeDesktop.Capture(window,token));lastFrame=png;
        var hit=TemplateMatcher.Find(png,File.ReadAllBytes(Path.Combine(directory,asset.File)),asset,window.Dpi,profile.Threshold,tight,asset.Scene==Scene.Success,role=="editor-export",role=="completion-actions",role=="completion-close");
        lastMiss=hit==null?$"Ảnh {role} chưa khớp ngưỡng {profile.Threshold:F2}; {dimensions}":"";
        await Task.CompletedTask;return hit==null?null:(window,hit);
    }
    private async Task<(WindowRef Window,TemplateHit Hit)> Locate(string role,CancellationToken token,bool tight=false,bool activateEditor=false)
    {
        var timer=Stopwatch.StartNew();var explained=false;lastFrame=null;
        while(timer.Elapsed<TimeSpan.FromSeconds(timeoutSeconds))
        {
            NativeDesktop.CheckStop(token);
            if(activateEditor)
            {
                var editor=NativeDesktop.Find(Scene.Editor);
                if(editor!=null)
                {
                    activateEditor=false;
                    if(!NativeDesktop.IsForegroundFor(editor))
                    {NativeDesktop.Focus(editor);log("Đã kích hoạt cửa sổ editor để tìm nút Xuất");await Task.Delay(300,token);}
                }
            }
            var hit=await Try(role,token,tight);if(hit!=null)return hit.Value;
            if(role=="editor-export"&&!explained&&timer.Elapsed>=TimeSpan.FromSeconds(5))
            {log("Đang chờ nút Xuất: "+lastMiss);explained=true;}
            await Task.Delay(500,token);
        }
        SaveDiagnostic(role);
        throw new TimeoutException(role=="editor-export"?"Chưa nhận diện được nút Xuất. "+lastMiss+". CapCut được giữ mở; chưa chạy project tiếp theo.":"Không thấy ảnh mẫu: "+role);
    }
    private void SaveDiagnostic(string role)
    {
        var folder=Path.Combine(directory,"diagnostics");Directory.CreateDirectory(folder);
        var path=Path.Combine(folder,DateTime.Now.ToString("yyyyMMdd-HHmmssfff")+"-"+role);
        if(lastFrame!=null)File.WriteAllBytes(path+".png",lastFrame);
        File.WriteAllText(path+".txt",lastMiss);log("Chẩn đoán "+role+": "+lastMiss+". Đã lưu tại "+path);
    }
    public async Task OpenExportDialog(CancellationToken token)
    {
        var activated=false;var misses=0;lastFrame=null;lastMiss="Chưa nhận diện editor";
        Task<bool> Visible(CancellationToken ct)
        {
            NativeDesktop.CheckStop(ct);
            // Stop retries as soon as the native dialog exists, even while its contents are loading.
            return Task.FromResult(NativeDesktop.Find(Scene.Export)!=null);
        }
        void Miss(string reason)
        {lastMiss=reason;if(++misses==2)log("Đang thử mở bảng xuất: "+lastMiss);}
        async Task Attempt(CancellationToken ct)
        {
            if(await Visible(ct))return;
            var editor=NativeDesktop.Find(Scene.Editor);
            if(editor==null){Miss("Chưa nhận diện cửa sổ editor");return;}
            NativeDesktop.CheckStop(ct);
            if(!activated)
            {
                activated=true;
                if(!NativeDesktop.IsForegroundFor(editor))NativeDesktop.Focus(editor);
            }
            if(!NativeDesktop.IsForegroundFor(editor)){Miss("Editor chưa ở phía trước");return;}
            // Reset hover before each new image match; this sends no click.
            NativeDesktop.Point(editor.Bounds.X+editor.Bounds.Width/2,editor.Bounds.Y+8,ct);
            await Task.Delay(150,ct);
            if(await Visible(ct))return;
            var first=await Try("editor-export",ct);
            if(first==null)
            {Miss(lastMiss);return;}
            await Task.Delay(200,ct);
            if(await Visible(ct))return;
            var second=await Try("editor-export",ct);
            if(second==null)return;
            var(w,h)=first.Value;var(w2,h2)=second.Value;
            if(w2.Handle!=w.Handle||w2.Bounds!=w.Bounds||Math.Abs(h2.X-h.X)>3||Math.Abs(h2.Y-h.Y)>3)
            {lastMiss="Nút Xuất còn thay đổi vị trí; chờ lần thử tiếp theo";return;}
            if(await Visible(ct))return;
            NativeDesktop.Click(w2.Bounds.X+h2.X,w2.Bounds.Y+h2.Y,ct);
            lastMiss="Đã bấm nút Xuất; bảng xuất chưa xuất hiện";log("Đã bấm nút Xuất trong editor");
        }
        try{await ExportDialogOpener.Open(Visible,Attempt,Pause,token);}
        catch(TimeoutException ex)
        {SaveDiagnostic("editor-export");throw new TimeoutException(ex.Message+" "+lastMiss,ex);}
    }
    public async Task Wait(string role,CancellationToken token)=>_ = await Locate(role,token,activateEditor:role=="editor-export");
    public async Task Preview(string role,CancellationToken token)
    {var(w,h)=await Locate(role,token,activateEditor:role=="editor-export");NativeDesktop.Point(w.Bounds.X+h.X,w.Bounds.Y+h.Y,token);log($"{role}: khớp {h.Score:F3}, điểm ({h.X:F0},{h.Y:F0}) trong cửa sổ; chưa bấm.");}
    public async Task Click(string role,CancellationToken token)
    {
        // Verify two stable frames BEFORE moving onto the button: hovering can change its image.
        var(w,h)=await Locate(role,token);await Task.Delay(200,token);
        var(w2,h2)=await Locate(role,token);
        if(w2.Handle!=w.Handle||w2.Bounds!=w.Bounds||Math.Abs(h2.X-h.X)>3||Math.Abs(h2.Y-h.Y)>3)throw new InvalidOperationException("Vị trí nút thay đổi. Không bấm.");
        NativeDesktop.Click(w.Bounds.X+h.X,w.Bounds.Y+h.Y,token);
        await Task.Delay(250,token);log("Đã bấm ảnh: "+role);
    }
    public async Task Search(string name,CancellationToken token)
    {
        await Click("search-toggle",token);
        // CapCut focuses the input when its search icon is clicked. Type there directly.
        await NativeDesktop.Fill(name,token);await Task.Delay(1000,token);
    }
    public async Task Pause(TimeSpan duration,CancellationToken token)
    {
        // Loading can temporarily change the foreground window; do not require editor focus here.
        var timer=Stopwatch.StartNew();
        while(timer.Elapsed<duration)
        {
            NativeDesktop.CheckStop(token);
            await Task.Delay(TimeSpan.FromMilliseconds(Math.Clamp((duration-timer.Elapsed).TotalMilliseconds,1,250)),token);
        }
        NativeDesktop.CheckStop(token);
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
    public async Task<bool> Success(CancellationToken token)
    {
        NativeDesktop.CheckStop(token);
        var window=NativeDesktop.Find(Scene.Success);
        if(window==null)return false;
        // The former Export button can become Share at the same coordinates; remove hover before matching.
        if(!NativeDesktop.IsForegroundFor(window))return false;
        NativeDesktop.Point(window.Bounds.X+window.Bounds.Width/2,window.Bounds.Y+8,token);
        await Task.Delay(150,token);
        return await Try("completion-close",token)!=null || await Try("completion-actions",token)!=null;
    }
    public Task Restart(string executable,CancellationToken token)=>NativeDesktop.Restart(executable,token);
}
