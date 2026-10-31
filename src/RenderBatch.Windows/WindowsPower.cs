using System.Diagnostics;
using System.IO;

namespace RenderBatch.Windows;

public static class WindowsPower
{
    // Countdown runs inside RenderBatch; stopping/closing it leaves no scheduled OS shutdown.
    public static async Task Shutdown(CancellationToken token)
    {
        NativeDesktop.CheckStop(token);
        var command = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "shutdown.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        command.ArgumentList.Add("/s");
        command.ArgumentList.Add("/t");
        command.ArgumentList.Add("0");
        // Do not force-close other applications with unsaved work.
        using var process = Process.Start(command) ?? throw new InvalidOperationException("Không khởi chạy được lệnh tắt máy Windows.");
        // After dispatch, Windows owns the request; cancellation is available during our countdown.
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Windows không chấp nhận lệnh tắt máy (mã {process.ExitCode}). Các project vẫn giữ trạng thái hoàn tất.");
    }
}
