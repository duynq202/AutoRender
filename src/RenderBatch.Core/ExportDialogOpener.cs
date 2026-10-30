namespace RenderBatch.Core;

public static class ExportDialogOpener
{
    public static async Task Open(Func<CancellationToken, Task<bool>> visible,
        Func<CancellationToken, Task> attempt, Func<TimeSpan, CancellationToken, Task> pause,
        CancellationToken token, TimeProvider? clock = null)
    {
        var time = clock ?? TimeProvider.System;
        var limit = TimeSpan.FromSeconds(120);
        using var deadline = new CancellationTokenSource(limit, time);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, deadline.Token);
        var cancellation = linked.Token;
        var started = time.GetTimestamp();
        var nextAttempt = TimeSpan.Zero;
        try
        {
            while (true)
            {
                cancellation.ThrowIfCancellationRequested();
                if (await visible(cancellation))
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (time.GetElapsedTime(started) <= limit) return;
                }
                var elapsed = time.GetElapsedTime(started);
                if (elapsed >= limit) throw TimedOut();
                if (elapsed >= nextAttempt)
                {
                    await attempt(cancellation);
                    // Skip missed slots instead of firing several clicks in a burst.
                    nextAttempt = TimeSpan.FromSeconds((Math.Floor(time.GetElapsedTime(started).TotalSeconds / 5) + 1) * 5);
                    continue; // Detect a newly opened dialog before waiting or clicking again.
                }
                var remaining = limit - elapsed;
                var wait = nextAttempt - elapsed;
                if (wait > TimeSpan.FromMilliseconds(500)) wait = TimeSpan.FromMilliseconds(500);
                if (wait > remaining) wait = remaining;
                await pause(wait, cancellation);
            }
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested && !token.IsCancellationRequested)
        {
            throw TimedOut();
        }
    }
    private static TimeoutException TimedOut() => new("Không mở được bảng xuất sau 120 giây; CapCut được giữ mở.");
}
