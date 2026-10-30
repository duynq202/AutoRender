using RenderBatch.Core;
using Xunit;
namespace RenderBatch.Tests;
public sealed class QueueTests:IDisposable
{
 private readonly string root=Path.Combine(Path.GetTempPath(),"renderbatch-"+Guid.NewGuid().ToString("N"));
 private Settings Settings=>new("CapCut.exe",root,SimpleWorkflow:false);
 public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
 [Theory][InlineData("Xuất-1027","1027",true)][InlineData("Export - KB_01","KB_01",true)][InlineData("Xuất-KB_01.3h","KB_01",false)][InlineData("CapCut","1027",false)]
 public void ExportTitleConfirmsExactProject(string title,string name,bool expected)=>Assert.Equal(expected,Validation.ExportTitleMatches(title,name));
 [Fact]public void PlaylistKeepsUnicodeAndOrder()=>Assert.Equal(new[]{"Dự án 1","KB_04"},Validation.Names("\uFEFFDự án 1\r\n\nKB_04\n"));
 [Theory][InlineData("A\na")][InlineData("CON")][InlineData("A/B")][InlineData("")]
 public void InvalidNamesRejected(string text)=>Assert.Throws<ArgumentException>(()=>Validation.Names(text));
 [Fact]public async Task QueueIsSequentialAndWritesOnlyVerifiedOutputs()
 {var s=new Session(Settings,[new("one"),new("two")]);var fake=new Fake(Settings);await new QueueRunner(fake,_=>{},_=>{}).Run(s,CancellationToken.None);Assert.Equal(new[]{"search:one","start:one","restart:one","search:two","start:two","restart:two"},fake.Events);Assert.All(s.Jobs,j=>Assert.Equal(JobState.Complete,j.State));}
 [Fact]public async Task WrongFirstSearchResultStopsBeforeExport()
 {var s=new Session(Settings,[new("one"),new("two")]);var fake=new Fake(Settings){WrongTitle=true};await Assert.ThrowsAsync<InvalidOperationException>(()=>new QueueRunner(fake,_=>{},_=>{}).Run(s,CancellationToken.None));Assert.DoesNotContain("start:one",fake.Events);Assert.Equal(JobState.Review,s.Jobs[0].State);Assert.Equal(JobState.Pending,s.Jobs[1].State);}
 [Fact]public async Task WrongOutputPathStopsBeforeExport()
 {var s=new Session(Settings,[new("one")]);var fake=new Fake(Settings){WrongPath=true};await Assert.ThrowsAsync<InvalidOperationException>(()=>new QueueRunner(fake,_=>{},_=>{}).Run(s,CancellationToken.None));Assert.DoesNotContain("start:one",fake.Events);}
 [Fact]public async Task ExistingOutputIsNeverOverwritten()
 {Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"one.mp4"),"existing");var s=new Session(Settings,[new("one")]);var fake=new Fake(Settings);await Assert.ThrowsAsync<IOException>(()=>new QueueRunner(fake,_=>{},_=>{}).Run(s,CancellationToken.None));Assert.Equal("existing",File.ReadAllText(Path.Combine(root,"one.mp4")));Assert.Empty(fake.Events);}
 [Fact]public async Task CancelPreservesStateAndDoesNotStartNextProject()
 {using var c=new CancellationTokenSource();var s=new Session(Settings,[new("one"),new("two")]);var fake=new Fake(Settings){Cancel=c};await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>new QueueRunner(fake,_=>{},_=>{}).Run(s,c.Token));Assert.Equal(JobState.Review,s.Jobs[0].State);Assert.DoesNotContain("search:two",fake.Events);}
 [Fact]public async Task CompletedJobsAreSkippedOnResume()
 {var s=new Session(Settings,[new("one",JobState.Complete),new("two")]);var fake=new Fake(Settings);await new QueueRunner(fake,_=>{},_=>{}).Run(s,CancellationToken.None);Assert.DoesNotContain("search:one",fake.Events);}
 [Fact]public async Task TimeoutRequiresPositiveSuccessEvenIfFileExists()
 {var s=new Session(Settings with{ExportMinutes=1},[new("one"),new("two")]);var clock=new ManualClock();var fake=new Fake(s.Settings){NeverSuccess=true,Clock=clock};await Assert.ThrowsAsync<TimeoutException>(()=>new QueueRunner(fake,_=>{},_=>{},clock).Run(s,CancellationToken.None));Assert.Equal(JobState.Review,s.Jobs[0].State);Assert.Equal(JobState.Pending,s.Jobs[1].State);}
 [Fact]public async Task FailedRestartPreservesCompletedVideo()
 {var s=new Session(Settings,[new("one"),new("two")]);var fake=new Fake(Settings){FailRestart=true};await Assert.ThrowsAsync<InvalidOperationException>(()=>new QueueRunner(fake,_=>{},_=>{}).Run(s,CancellationToken.None));Assert.Equal(JobState.Complete,s.Jobs[0].State);Assert.Equal(JobState.Pending,s.Jobs[1].State);}
 [Fact]public void SimpleWorkflowNeedsOnlySixImages()
 {var roles=Roles.Required(Settings with{SimpleWorkflow=true}).ToArray();Assert.Equal(new[]{"home","search-toggle","first-result","editor-export","confirm-export","success"},roles);Assert.DoesNotContain("close-success",Roles.Required(Settings));}
 [Fact]public async Task SimpleWorkflowUsesExistingCapCutConfigurationAndRestartsEachTime()
 {var settings=Settings with{SimpleWorkflow=true};var s=new Session(settings,[new("one"),new("two")]);var fake=new Fake(settings);await new QueueRunner(fake,_=>{},_=>{}).Run(s,CancellationToken.None);Assert.Equal(new[]{"search:one","start:one","restart:one","search:two","start:two","restart:two"},fake.Events);Assert.DoesNotContain(fake.Actions,a=>a.StartsWith("fill:")||a.StartsWith("select:")||a=="folder");Assert.DoesNotContain("click:close-success",fake.Actions);}
 [Fact]public async Task SimpleWorkflowStillRejectsWrongProject()
 {var settings=Settings with{SimpleWorkflow=true};var s=new Session(settings,[new("one"),new("two")]);var fake=new Fake(settings){WrongTitle=true};await Assert.ThrowsAsync<InvalidOperationException>(()=>new QueueRunner(fake,_=>{},_=>{}).Run(s,CancellationToken.None));Assert.DoesNotContain("start:one",fake.Events);Assert.DoesNotContain("restart:one",fake.Events);Assert.Equal(JobState.Pending,s.Jobs[1].State);}
 [Fact]public async Task SuccessPictureWithoutVideoNeverClosesCapCutOrStartsNextProject()
 {var settings=Settings with{SimpleWorkflow=true,ExportMinutes=1};var s=new Session(settings,[new("one"),new("two")]);var clock=new ManualClock();var fake=new Fake(settings){NoOutput=true,Clock=clock};await Assert.ThrowsAsync<TimeoutException>(()=>new QueueRunner(fake,_=>{},_=>{},clock).Run(s,CancellationToken.None));Assert.DoesNotContain("restart:one",fake.Events);Assert.Equal(JobState.Review,s.Jobs[0].State);Assert.Equal(JobState.Pending,s.Jobs[1].State);}
 [Fact]public async Task ProjectGets180SecondsToLoadBeforeLookingForExport()
 {var s=new Session(Settings,[new("one")]);var fake=new Fake(Settings);await new QueueRunner(fake,_=>{},_=>{}).Run(s,CancellationToken.None);var opened=fake.Actions.IndexOf("click:first-result");var paused=fake.Actions.IndexOf("pause:180");var waited=fake.Actions.IndexOf("wait:editor-export");var clicked=fake.Actions.IndexOf("click:editor-export");Assert.True(opened>=0&&opened<paused&&paused<waited&&waited<clicked);}
 [Fact]public async Task CancelDuringProjectLoadingNeverClicksExportOrStartsNextProject()
 {using var c=new CancellationTokenSource();var s=new Session(Settings,[new("one"),new("two")]);var fake=new Fake(Settings){CancelOnOpening=c};await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>new QueueRunner(fake,_=>{},_=>{}).Run(s,c.Token));Assert.DoesNotContain("click:editor-export",fake.Actions);Assert.DoesNotContain("restart:one",fake.Events);Assert.Equal(JobState.Review,s.Jobs[0].State);Assert.Equal(JobState.Pending,s.Jobs[1].State);}
 [Fact]public async Task ExportScansOncePerMinuteAndPollingWritesNoLogs()
 {var s=new Session(Settings,[new("one")]);var clock=new ManualClock();var fake=new Fake(Settings){Clock=clock,SuccessAfterPoll=3};var logs=new List<string>();await new QueueRunner(fake,_=>{},logs.Add,clock).Run(s,CancellationToken.None);Assert.Equal(new double[]{60,120,180},fake.PollSeconds);var start=logs.FindIndex(x=>x.Contains("Đang xuất;"));var complete=logs.FindIndex(x=>x.Contains("file có dữ liệu"));Assert.Equal(start+1,complete);Assert.Equal(JobState.Complete,s.Jobs[0].State);}
 [Fact]public async Task ScanProcessingTimeDoesNotAccumulateIntoMinuteIntervals()
 {var s=new Session(Settings,[new("one")]);var clock=new ManualClock();var fake=new Fake(Settings){Clock=clock,SuccessAfterPoll=3,ScanDuration=TimeSpan.FromSeconds(10)};await new QueueRunner(fake,_=>{},_=>{},clock).Run(s,CancellationToken.None);Assert.Equal(new double[]{60,120,180},fake.PollSeconds);}
 [Fact]public async Task ExportTimeoutIs180MinutesAndNeverStartsNextProject()
 {Assert.Equal(180,Settings.ExportMinutes);var s=new Session(Settings,[new("one"),new("two")]);var clock=new ManualClock();var fake=new Fake(Settings){Clock=clock,NeverSuccess=true};await Assert.ThrowsAsync<TimeoutException>(()=>new QueueRunner(fake,_=>{},_=>{},clock).Run(s,CancellationToken.None));Assert.Equal(180,fake.PollSeconds.Count);Assert.Equal(60,fake.PollSeconds[0]);Assert.Equal(10800,fake.PollSeconds[^1]);Assert.DoesNotContain("restart:one",fake.Events);Assert.Equal(JobState.Pending,s.Jobs[1].State);}
 [Fact]public async Task SuccessOnFinalMinuteIsAccepted()
 {var s=new Session(Settings,[new("one")]);var clock=new ManualClock();var fake=new Fake(Settings){Clock=clock,SuccessAfterPoll=180};await new QueueRunner(fake,_=>{},_=>{},clock).Run(s,CancellationToken.None);Assert.Equal(10800,fake.PollSeconds[^1]);Assert.Equal(JobState.Complete,s.Jobs[0].State);}
 [Fact]public async Task CancelDuringMinuteWaitDoesNotScanOrCloseCapCut()
 {using var c=new CancellationTokenSource();var s=new Session(Settings,[new("one"),new("two")]);var clock=new ManualClock();var fake=new Fake(Settings){Clock=clock,CancelOnPolling=c};await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>new QueueRunner(fake,_=>{},_=>{},clock).Run(s,c.Token));Assert.Empty(fake.PollSeconds);Assert.DoesNotContain("restart:one",fake.Events);Assert.Equal(JobState.Review,s.Jobs[0].State);Assert.Equal(JobState.Pending,s.Jobs[1].State);}
 [Fact]public void ExportWaitCannotExceed180Minutes()=>Assert.Throws<ArgumentException>(()=>Validation.Settings(Settings with{ExportMinutes=181}));
 private sealed class ManualClock:TimeProvider
 {private long ticks;public override long TimestampFrequency=>TimeSpan.TicksPerSecond;public override long GetTimestamp()=>ticks;public void Advance(TimeSpan duration)=>ticks+=duration.Ticks;}
 private sealed class Fake(Settings settings):IDesktop
 {
  public List<string> Events=new();public List<string> Actions=new();public bool WrongTitle,WrongPath,NeverSuccess,NoOutput,FailRestart;public CancellationTokenSource? Cancel,CancelOnOpening,CancelOnPolling;public ManualClock? Clock;public List<double> PollSeconds=new();public int SuccessAfterPoll=1;public TimeSpan ScanDuration;private string name="";private bool started;private long exportStarted;
  public Task Wait(string role,CancellationToken token){token.ThrowIfCancellationRequested();Actions.Add("wait:"+role);return Task.CompletedTask;}
  public Task Click(string role,CancellationToken token){token.ThrowIfCancellationRequested();Actions.Add("click:"+role);if(role=="confirm-export"){started=true;exportStarted=Clock?.GetTimestamp()??0;Events.Add("start:"+name);if(!NoOutput)File.WriteAllText(Path.Combine(settings.OutputFolder,name+".mp4"),"video");Cancel?.Cancel();}return Task.CompletedTask;}
  public Task Search(string n,CancellationToken token){name=n;started=false;Events.Add("search:"+name);return Task.CompletedTask;}
  public Task Pause(TimeSpan duration,CancellationToken token){Actions.Add("pause:"+duration.TotalSeconds);if(started)CancelOnPolling?.Cancel();else CancelOnOpening?.Cancel();token.ThrowIfCancellationRequested();Clock?.Advance(duration);return Task.CompletedTask;}
  public Task<string> ExportTitle(CancellationToken token)=>Task.FromResult("Xuất-"+(WrongTitle?"wrong":name));
  public Task Fill(string role,string value,CancellationToken token){Actions.Add("fill:"+role);return Task.CompletedTask;}
  public Task<string> Read(string role,CancellationToken token)=>Task.FromResult(Path.Combine(settings.OutputFolder,(WrongPath?"wrong":name)+".mp4"));
  public Task ChooseFolder(string folder,CancellationToken token){Actions.Add("folder");return Task.CompletedTask;}
  public Task Select(string menu,string value,CancellationToken token){Actions.Add("select:"+menu);return Task.CompletedTask;}
  public Task<bool> Success(CancellationToken token){token.ThrowIfCancellationRequested();if(started){PollSeconds.Add(Clock?.GetElapsedTime(exportStarted).TotalSeconds??0);Clock?.Advance(ScanDuration);}return Task.FromResult(started&&!NeverSuccess&&PollSeconds.Count>=SuccessAfterPoll);}
  public Task Restart(string executable,CancellationToken token){token.ThrowIfCancellationRequested();Events.Add("restart:"+name);if(FailRestart)throw new InvalidOperationException("Restart failed");return Task.CompletedTask;}
 }
}
