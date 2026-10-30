using RenderBatch.Core;
using Xunit;
namespace RenderBatch.Tests;

public sealed class ExportDialogOpenerTests
{
    private sealed class Clock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => ticks;
        public double Seconds => TimeSpan.FromTicks(ticks).TotalSeconds;
        public void Advance(TimeSpan duration) => ticks += duration.Ticks;
        public Task Pause(TimeSpan duration, CancellationToken token)
        {token.ThrowIfCancellationRequested();Advance(duration);return Task.CompletedTask;}
    }
    [Fact] public async Task ExistingDialogIsNeverClickedAgain()
    {
        var clock=new Clock();var clicks=0;
        await ExportDialogOpener.Open(_=>Task.FromResult(true),_=>{clicks++;return Task.CompletedTask;},clock.Pause,CancellationToken.None,clock);
        Assert.Equal(0,clicks);Assert.Equal(0,clock.Seconds);
    }
    [Fact] public async Task ClicksEveryFiveSecondsAndStopsAsSoonAsDialogAppears()
    {
        var clock=new Clock();var clicks=new List<double>();
        await ExportDialogOpener.Open(_=>Task.FromResult(clock.Seconds>=12),_=>{clicks.Add(clock.Seconds);return Task.CompletedTask;},clock.Pause,CancellationToken.None,clock);
        Assert.Equal(new double[]{0,5,10},clicks);Assert.Equal(12,clock.Seconds);
    }
    [Fact] public async Task ImmediateResponseDoesNotWaitForAnotherRetry()
    {
        var clock=new Clock();var visible=false;var clicks=0;
        await ExportDialogOpener.Open(_=>Task.FromResult(visible),_=>{clicks++;visible=true;return Task.CompletedTask;},clock.Pause,CancellationToken.None,clock);
        Assert.Equal(1,clicks);Assert.Equal(0,clock.Seconds);
    }
    [Fact] public async Task MissingDialogTimesOutAt120SecondsWith24Attempts()
    {
        var clock=new Clock();var clicks=new List<double>();
        await Assert.ThrowsAsync<TimeoutException>(()=>ExportDialogOpener.Open(_=>Task.FromResult(false),_=>{clicks.Add(clock.Seconds);return Task.CompletedTask;},clock.Pause,CancellationToken.None,clock));
        Assert.Equal(120,clock.Seconds);Assert.Equal(Enumerable.Range(0,24).Select(i=>(double)i*5),clicks);
    }
    [Fact] public async Task SlowAttemptsSkipMissedSlotsWithoutBurstClicks()
    {
        var clock=new Clock();var clicks=new List<double>();
        await ExportDialogOpener.Open(_=>Task.FromResult(clock.Seconds>=25),_=>{clicks.Add(clock.Seconds);clock.Advance(TimeSpan.FromSeconds(7));return Task.CompletedTask;},clock.Pause,CancellationToken.None,clock);
        Assert.Equal(new double[]{0,10,20},clicks);
    }
    [Fact] public async Task CancellationDuringIntervalPreventsFurtherClicks()
    {
        var clock=new Clock();var clicks=0;using var cancel=new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>ExportDialogOpener.Open(_=>Task.FromResult(false),_=>{clicks++;return Task.CompletedTask;},(duration,ct)=>{cancel.Cancel();ct.ThrowIfCancellationRequested();return Task.CompletedTask;},cancel.Token,clock));
        Assert.Equal(1,clicks);
    }
    [Fact] public async Task UnsafeAttemptIsNotRetried()
    {
        var clock=new Clock();var clicks=0;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>ExportDialogOpener.Open(_=>Task.FromResult(false),_=>{clicks++;throw new InvalidOperationException("Wrong foreground application");},clock.Pause,CancellationToken.None,clock));
        Assert.Equal(1,clicks);
    }
}
