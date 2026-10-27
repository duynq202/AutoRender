using AutoRender.Core;
using Xunit;
namespace AutoRender.Tests;

public sealed class QueueTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "autorender-test-" + Guid.NewGuid().ToString("N"));
    private JobStore Store() => new(Path.Combine(folder, "queue.db"));
    private ExportSettings Settings => new(Path.Combine(folder, "videos"), PollMilliseconds: 10);
    public QueueTests() => Directory.CreateDirectory(folder);
    public void Dispose() { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(folder, true); }

    [Fact] public void PlaylistPreservesUnicodeAndOrder()
        => Assert.Equal(new[] { "Dự án dài", "Project 2" }, Playlist.Parse("\uFEFFDự án dài\r\n\n Project 2 \n"));
    [Theory]
    [InlineData("One\none")][InlineData("A/B")][InlineData("CON")][InlineData("NUL.mp4")][InlineData("LPT1")][InlineData("name.")][InlineData("  \r\n")]
    public void InvalidPlaylistRejected(string text) => Assert.Throws<ArgumentException>(() => Playlist.Parse(text));

    [Fact] public async Task ExportsInOrderOnlyAfterCompletionAndClosedFile()
    {
        var store = Store(); store.Replace(["First", "Second"], Settings);
        var driver = new FakeDriver(); var runner = new QueueRunner(store, driver);
        await runner.RunAsync(CancellationToken.None);
        Assert.Equal(new[] { "open:First", "configure:First", "begin:First", "home:First",
            "open:Second", "configure:Second", "begin:Second", "home:Second" }, driver.Calls);
        Assert.All(store.Read(), j => Assert.Equal(JobState.Completed, j.State));
        Assert.Equal(4, driver.Polls);
    }
    [Fact] public async Task DoesNotOverwriteExistingVideo()
    {
        var store = Store(); store.Replace(["First", "Second"], Settings);
        Directory.CreateDirectory(Settings.OutputDirectory); var file = store.Read()[0].Output;
        File.WriteAllText(file, "original"); var driver = new FakeDriver();
        await Assert.ThrowsAsync<IOException>(() => new QueueRunner(store, driver).RunAsync(CancellationToken.None));
        Assert.Equal("original", File.ReadAllText(file)); Assert.Empty(driver.Calls);
        Assert.Equal(JobState.Failed, store.Read()[0].State); Assert.Equal(JobState.Pending, store.Read()[1].State);
    }
    [Fact] public async Task FailureStopsBeforeNextProject()
    {
        var store = Store(); store.Replace(["First", "Second"], Settings);
        var driver = new FakeDriver { FailOpening = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new QueueRunner(store, driver).RunAsync(CancellationToken.None));
        Assert.Single(driver.Calls); Assert.Equal(JobState.Failed, store.Read()[0].State);
        Assert.Equal(JobState.Pending, store.Read()[1].State);
    }
    [Fact] public async Task CancellationDuringExportRequiresReview()
    {
        var store = Store(); store.Replace(["First", "Second"], Settings);
        using var cancel = new CancellationTokenSource();
        var driver = new FakeDriver { OnBegin = cancel.Cancel };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new QueueRunner(store, driver).RunAsync(cancel.Token));
        Assert.Equal(JobState.NeedsReview, store.Read()[0].State);
        Assert.Equal(JobState.Pending, store.Read()[1].State);
        Assert.DoesNotContain("open:Second", driver.Calls);
    }
    [Fact] public void InterruptedJobsRecoveredWithoutChangingCompletedJobs()
    {
        var store = Store(); store.Replace(["First", "Second"], Settings);
        var jobs = store.Read(); store.Update(jobs[0].Id, JobState.Completed, "done");
        store.Update(jobs[1].Id, JobState.Exporting, "working");
        var reopened = Store(); reopened.RecoverInterrupted();
        Assert.Equal(JobState.Completed, reopened.Read()[0].State);
        Assert.Equal(JobState.NeedsReview, reopened.Read()[1].State); Assert.Equal(Settings, reopened.Settings());
    }
    [Fact] public async Task CompletedJobsAreNotExportedAgain()
    {
        var store = Store(); store.Replace(["First", "Second"], Settings);
        store.Update(store.Read()[0].Id, JobState.Completed, "done"); var driver = new FakeDriver();
        await new QueueRunner(store, driver).RunAsync(CancellationToken.None);
        Assert.DoesNotContain("open:First", driver.Calls); Assert.Contains("open:Second", driver.Calls);
    }
    [Fact] public async Task FailureToReturnHomePreservesCompletedStateAndStopsQueue()
    {
        var store = Store(); store.Replace(["First", "Second"], Settings);
        var driver = new FakeDriver { FailHome = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new QueueRunner(store, driver).RunAsync(CancellationToken.None));
        Assert.Equal(JobState.Completed, store.Read()[0].State); Assert.Equal(JobState.Pending, store.Read()[1].State);
    }
    [Fact] public void MissingAndEmptyFilesDoNotCountAsCompleted()
    {
        var path = Path.Combine(folder, "test.mp4"); Assert.False(QueueRunner.OutputReady(path));
        File.WriteAllText(path, ""); Assert.False(QueueRunner.OutputReady(path));
        File.WriteAllText(path, "data"); Assert.True(QueueRunner.OutputReady(path));
        using var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.False(QueueRunner.OutputReady(path));
    }
    [Fact] public async Task TimeoutStopsWithoutOpeningNextProject()
    {
        var store = Store(); store.Replace(["First", "Second"], Settings with { TimeoutMinutes = 1 });
        var driver = new FakeDriver { NeverComplete = true };
        await Assert.ThrowsAsync<TimeoutException>(() => new QueueRunner(store, driver, new FastClock()).RunAsync(CancellationToken.None));
        Assert.Equal(JobState.Failed, store.Read()[0].State);
        Assert.Equal(JobState.Pending, store.Read()[1].State);
        Assert.DoesNotContain("open:Second", driver.Calls);
    }
    private sealed class FastClock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency => 1;
        public override long GetTimestamp() => Interlocked.Add(ref ticks, 61);
    }
    private sealed class FakeDriver : ICapCutDriver
    {
        public List<string> Calls { get; } = new();
        public bool FailOpening, FailHome, NeverComplete;
        public Action? OnBegin;
        public int Polls;
        private int projectPolls;
        private string name = "", output = "";
        public Task OpenProjectAsync(string name, ExportSettings settings, CancellationToken token)
        {
            Calls.Add("open:" + name); this.name = name; projectPolls = 0;
            if (FailOpening) throw new InvalidOperationException("Project not found");
            return Task.CompletedTask;
        }
        public Task ConfigureAsync(string name, ExportSettings settings, CancellationToken token)
        { Calls.Add("configure:" + name); output = Path.Combine(settings.OutputDirectory, name + ".mp4"); return Task.CompletedTask; }
        public Task BeginExportAsync(CancellationToken token) { Calls.Add("begin:" + name); OnBegin?.Invoke(); return Task.CompletedTask; }
        public Task<bool> IsExportCompleteAsync(CancellationToken token)
        {
            Polls++; projectPolls++;
            if (NeverComplete || projectPolls < 2) return Task.FromResult(false);
            File.WriteAllText(output, "test-video-placeholder"); return Task.FromResult(true);
        }
        public Task ReturnHomeAsync(CancellationToken token)
        { Calls.Add("home:" + name); if (FailHome) throw new InvalidOperationException("Cannot return home"); return Task.CompletedTask; }
    }
}
