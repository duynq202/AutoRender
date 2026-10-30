using RenderBatch.Core;
using Xunit;

namespace RenderBatch.Tests;

public sealed class CapCutRestarterTests
{
    [Fact]
    public async Task SharePanelOrBackgroundProcessCannotPreventRestart()
    {
        var fake = new Lifecycle();
        await fake.Run();
        Assert.Equal(new[] { "terminate", "start" }, fake.Events);
        Assert.Equal(0, fake.Clock.Seconds);
    }

    [Fact]
    public async Task FailedTerminationNeverLaunchesASecondInstance()
    {
        var fake = new Lifecycle { ExitOnTerminate = false };
        await Assert.ThrowsAsync<TimeoutException>(() => fake.Run());
        Assert.Equal(new[] { "terminate" }, fake.Events);
        Assert.Equal(15, fake.Clock.Seconds);
    }

    [Fact]
    public async Task CancellationBeforeTerminationDoesNotCloseOrLaunch()
    {
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        var fake = new Lifecycle();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fake.Run(cancel.Token));
        Assert.Empty(fake.Events);
    }

    [Fact]
    public async Task CancellationWhileWaitingForExitDoesNotLaunch()
    {
        using var cancel = new CancellationTokenSource();
        var fake = new Lifecycle { ExitOnTerminate = false, CancelAt = 1, Cancel = cancel };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fake.Run(cancel.Token));
        Assert.Equal(new[] { "terminate" }, fake.Events);
    }

    [Fact]
    public async Task CancellationAfterTerminationDoesNotLaunch()
    {
        using var cancel = new CancellationTokenSource();
        var fake = new Lifecycle { CancelOnTerminate = cancel };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fake.Run(cancel.Token));
        Assert.Equal(new[] { "terminate" }, fake.Events);
    }

    [Fact]
    public async Task AlreadyExitedSessionOnlyStartsOnce()
    {
        var fake = new Lifecycle { Exited = true };
        await fake.Run();
        Assert.Equal(new[] { "start" }, fake.Events);
    }

    [Fact]
    public async Task AccessDeniedOnTerminationIsReportedWithoutLaunching()
    {
        var fake = new Lifecycle { DenyTermination = true };
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fake.Run());
        Assert.Equal(new[] { "terminate" }, fake.Events);
    }

    private sealed class Clock : TimeProvider
    {
        private long ticks;
        public double Seconds => TimeSpan.FromTicks(ticks).TotalSeconds;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => ticks;
        public void Advance(TimeSpan duration) => ticks += duration.Ticks;
    }

    private sealed class Lifecycle
    {
        public readonly List<string> Events = [];
        public readonly Clock Clock = new();
        public bool Exited, ExitOnTerminate = true, DenyTermination;
        public double CancelAt = double.PositiveInfinity;
        public CancellationTokenSource? Cancel, CancelOnTerminate;
        public Task Run(CancellationToken token = default) => CapCutRestarter.Run(
            () => Exited,
            ct =>
            {
                ct.ThrowIfCancellationRequested(); Events.Add("terminate");
                if (DenyTermination) throw new UnauthorizedAccessException();
                Exited = ExitOnTerminate; CancelOnTerminate?.Cancel(); return Task.CompletedTask;
            },
            ct => { ct.ThrowIfCancellationRequested(); Assert.True(Exited); Events.Add("start"); return Task.CompletedTask; },
            (duration, ct) => { ct.ThrowIfCancellationRequested(); Clock.Advance(duration); if (Clock.Seconds >= CancelAt) Cancel?.Cancel(); return Task.CompletedTask; },
            token, Clock);
    }
}
