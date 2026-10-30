using RenderBatch.Core;
using Xunit;

namespace RenderBatch.Tests;

public sealed class CapCutRestarterTests
{
    [Fact]
    public async Task NormalExitStartsOnceWithoutTermination()
    {
        var fake = new Lifecycle { ExitOnClose = true };
        await fake.Run();
        Assert.Equal(new[] { "close", "start" }, fake.Events);
        Assert.Equal(0, fake.Clock.Seconds);
    }

    [Fact]
    public async Task SharePanelOrBackgroundProcessCannotPreventRestart()
    {
        var fake = new Lifecycle();
        await fake.Run();
        Assert.Equal(new[] { "close", "terminate", "start" }, fake.Events);
        Assert.Equal(10, fake.Clock.Seconds);
    }

    [Fact]
    public async Task FailedTerminationNeverLaunchesASecondInstance()
    {
        var fake = new Lifecycle { ExitOnTerminate = false };
        await Assert.ThrowsAsync<TimeoutException>(() => fake.Run());
        Assert.Equal(new[] { "close", "terminate" }, fake.Events);
        Assert.Equal(25, fake.Clock.Seconds);
    }

    [Fact]
    public async Task CancellationBeforeFallbackDoesNotTerminateOrLaunch()
    {
        using var cancel = new CancellationTokenSource();
        var fake = new Lifecycle { CancelAt = 10, Cancel = cancel };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fake.Run(cancel.Token));
        Assert.Equal(new[] { "close" }, fake.Events);
    }

    [Fact]
    public async Task CancellationAfterTerminationDoesNotLaunch()
    {
        using var cancel = new CancellationTokenSource();
        var fake = new Lifecycle { CancelOnTerminate = cancel };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fake.Run(cancel.Token));
        Assert.Equal(new[] { "close", "terminate" }, fake.Events);
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
        Assert.Equal(new[] { "close", "terminate" }, fake.Events);
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
        public bool Exited, ExitOnClose, ExitOnTerminate = true, DenyTermination;
        public double CancelAt = double.PositiveInfinity;
        public CancellationTokenSource? Cancel, CancelOnTerminate;
        public Task Run(CancellationToken token = default) => CapCutRestarter.Run(
            ct => { ct.ThrowIfCancellationRequested(); Events.Add("close"); Exited = ExitOnClose; return Task.CompletedTask; },
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
