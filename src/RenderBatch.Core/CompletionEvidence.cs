namespace RenderBatch.Core;

public sealed record CompletionEvidence(long WindowHandle, int ProcessId, long ProcessStartedTicks, double X, double Y)
{
    public bool Matches(CompletionEvidence? other) => other is not null &&
        WindowHandle == other.WindowHandle && ProcessId == other.ProcessId &&
        ProcessStartedTicks == other.ProcessStartedTicks &&
        Math.Abs(X - other.X) <= 3 && Math.Abs(Y - other.Y) <= 3;
}
