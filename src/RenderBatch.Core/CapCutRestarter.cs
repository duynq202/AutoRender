namespace RenderBatch.Core;

// Invoked only after positive completion-image and exclusive output-file proof.
public static class CapCutRestarter
{
    public static async Task Run(Func<CancellationToken, Task> requestClose,
        Func<bool> hasExited, Func<CancellationToken, Task> terminate,
        Func<CancellationToken, Task> start, Func<TimeSpan, CancellationToken, Task> pause,
        CancellationToken token, TimeProvider? clock = null)
    {
        var time = clock ?? TimeProvider.System;
        token.ThrowIfCancellationRequested();
        if (!hasExited()) await requestClose(token);
        if (!await WaitForExit(TimeSpan.FromSeconds(10)))
        {
            token.ThrowIfCancellationRequested();
            await terminate(token);
            if (!await WaitForExit(TimeSpan.FromSeconds(15)))
                throw new TimeoutException("CapCut chưa thoát; chưa mở phiên mới hoặc chạy project tiếp theo.");
        }
        token.ThrowIfCancellationRequested();
        await start(token);

        async Task<bool> WaitForExit(TimeSpan limit)
        {
            var started = time.GetTimestamp();
            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (hasExited()) return true;
                var remaining = limit - time.GetElapsedTime(started);
                if (remaining <= TimeSpan.Zero) return false;
                await pause(remaining < TimeSpan.FromMilliseconds(250) ? remaining : TimeSpan.FromMilliseconds(250), token);
            }
        }
    }
}
