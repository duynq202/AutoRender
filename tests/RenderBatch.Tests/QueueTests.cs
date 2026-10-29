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
 {var s=new Session(Settings with{ExportMinutes=1},[new("one"),new("two")]);var fake=new Fake(s.Settings){NeverSuccess=true};await Assert.ThrowsAsync<TimeoutException>(()=>new QueueRunner(fake,_=>{},_=>{},new FastClock()).Run(s,CancellationToken.None));Assert.Equal(JobState.Review,s.Jobs[0].State);Assert.Equal(JobState.Pending,s.Jobs[1].State);}
 [Fact]public async Task FailedRestartPreservesCompletedVideo()
 {var s=new Session(Settings,[new("one"),new("two")]);var fake=new Fake(Settings){FailRestart=true};await Assert.ThrowsAsync<InvalidOperationException>(()=>new QueueRunner(fake,_=>{},_=>{}).Run(s,CancellationToken.None));Assert.Equal(JobState.Complete,s.Jobs[0].State);Assert.Equal(JobState.Pending,s.Jobs[1].State);}
 [Fact]public void SimpleWorkflowNeedsOnlySevenImages()
 {var roles=Roles.Required(Settings with{SimpleWorkflow=true}).ToArray();Assert.Equal(new[]{"home","search-toggle","search-field","first-result","editor-export","confirm-export","success"},roles);Assert.DoesNotContain("close-success",Roles.Required(Settings));}
 [Fact]public async Task SimpleWorkflowUsesExistingCapCutConfigurationAndRestartsEachTime()
 {var settings=Settings with{SimpleWorkflow=true};var s=new Session(settings,[new("one"),new("two")]);var fake=new Fake(settings);await new QueueRunner(fake,_=>{},_=>{}).Run(s,CancellationToken.None);Assert.Equal(new[]{"search:one","start:one","restart:one","search:two","start:two","restart:two"},fake.Events);Assert.DoesNotContain(fake.Actions,a=>a.StartsWith("fill:")||a.StartsWith("select:")||a=="folder");Assert.DoesNotContain("click:close-success",fake.Actions);}
 [Fact]public async Task SimpleWorkflowStillRejectsWrongProject()
 {var settings=Settings with{SimpleWorkflow=true};var s=new Session(settings,[new("one"),new("two")]);var fake=new Fake(settings){WrongTitle=true};await Assert.ThrowsAsync<InvalidOperationException>(()=>new QueueRunner(fake,_=>{},_=>{}).Run(s,CancellationToken.None));Assert.DoesNotContain("start:one",fake.Events);Assert.DoesNotContain("restart:one",fake.Events);Assert.Equal(JobState.Pending,s.Jobs[1].State);}
 [Fact]public async Task SuccessPictureWithoutVideoNeverClosesCapCutOrStartsNextProject()
 {var settings=Settings with{SimpleWorkflow=true,ExportMinutes=1};var s=new Session(settings,[new("one"),new("two")]);var fake=new Fake(settings){NoOutput=true};await Assert.ThrowsAsync<TimeoutException>(()=>new QueueRunner(fake,_=>{},_=>{},new FastClock()).Run(s,CancellationToken.None));Assert.DoesNotContain("restart:one",fake.Events);Assert.Equal(JobState.Review,s.Jobs[0].State);Assert.Equal(JobState.Pending,s.Jobs[1].State);}
 private sealed class FastClock:TimeProvider{private long ticks;public override long TimestampFrequency=>1;public override long GetTimestamp()=>Interlocked.Add(ref ticks,61);}
 private sealed class Fake(Settings settings):IDesktop
 {
  public List<string> Events=new();public List<string> Actions=new();public bool WrongTitle,WrongPath,NeverSuccess,NoOutput,FailRestart;public CancellationTokenSource? Cancel;private string name="";private bool started;
  public Task Wait(string role,CancellationToken token){token.ThrowIfCancellationRequested();return Task.CompletedTask;}
  public Task Click(string role,CancellationToken token){token.ThrowIfCancellationRequested();Actions.Add("click:"+role);if(role=="confirm-export"){started=true;Events.Add("start:"+name);if(!NoOutput)File.WriteAllText(Path.Combine(settings.OutputFolder,name+".mp4"),"video");Cancel?.Cancel();}return Task.CompletedTask;}
  public Task Search(string n,CancellationToken token){name=n;started=false;Events.Add("search:"+name);return Task.CompletedTask;}
  public Task<string> ExportTitle(CancellationToken token)=>Task.FromResult("Xuất-"+(WrongTitle?"wrong":name));
  public Task Fill(string role,string value,CancellationToken token){Actions.Add("fill:"+role);return Task.CompletedTask;}
  public Task<string> Read(string role,CancellationToken token)=>Task.FromResult(Path.Combine(settings.OutputFolder,(WrongPath?"wrong":name)+".mp4"));
  public Task ChooseFolder(string folder,CancellationToken token){Actions.Add("folder");return Task.CompletedTask;}
  public Task Select(string menu,string value,CancellationToken token){Actions.Add("select:"+menu);return Task.CompletedTask;}
  public Task<bool> Success(CancellationToken token){token.ThrowIfCancellationRequested();return Task.FromResult(started&&!NeverSuccess);}
  public Task Restart(string executable,CancellationToken token){token.ThrowIfCancellationRequested();Events.Add("restart:"+name);if(FailRestart)throw new InvalidOperationException("Restart failed");return Task.CompletedTask;}
 }
}
