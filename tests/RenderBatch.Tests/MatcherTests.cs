using OpenCvSharp;
using RenderBatch.Core;
using Xunit;
namespace RenderBatch.Tests;
public sealed class MatcherTests
{
 private static byte[] Bytes(Mat m)=>m.ImEncode(".png");
 private static Mat Pattern(){var m=new Mat(24,28,MatType.CV_8UC1,Scalar.Black);Cv2.Line(m,new(2,3),new(24,18),Scalar.White,3);Cv2.Circle(m,new(10,12),5,new Scalar(160),2);return m;}
 private static Asset Asset()=>new("test",Scene.Home,"test.png",240,180,96,new(80,60,28,24),14,12,70);
 [Fact]public void FindsRealTemplateAndCalibratedTargetOffset()
 {using var pattern=Pattern();using var frame=new Mat(180,240,MatType.CV_8UC1,Scalar.Black);using(var roi=new Mat(frame,new Rect(91,70,28,24)))pattern.CopyTo(roi);var hit=TemplateMatcher.Find(Bytes(frame),Bytes(pattern),Asset(),96,.97);Assert.NotNull(hit);Assert.InRange(hit.Score,.999,1.001);Assert.Equal(105,hit.X);Assert.Equal(82,hit.Y);}
 [Fact]public void MissingTemplateDoesNotReturnNearestWeakCandidate()
 {using var pattern=Pattern();using var frame=new Mat(180,240,MatType.CV_8UC1,Scalar.Black);Assert.Null(TemplateMatcher.Find(Bytes(frame),Bytes(pattern),Asset(),96,.97));}
 [Fact]public void TwoIdenticalButtonsAreRejected()
 {using var pattern=Pattern();using var frame=new Mat(180,240,MatType.CV_8UC1,Scalar.Black);foreach(var p in new[]{new OpenCvSharp.Point(40,60),new OpenCvSharp.Point(120,60)}){using var roi=new Mat(frame,new Rect(p.X,p.Y,28,24));pattern.CopyTo(roi);}Assert.Throws<InvalidOperationException>(()=>TemplateMatcher.Find(Bytes(frame),Bytes(pattern),Asset(),96,.97));}
 [Fact]public void FlatTemplateCannotMatchEverything()
 {using var flat=new Mat(24,28,MatType.CV_8UC1,Scalar.White);using var frame=new Mat(180,240,MatType.CV_8UC1,Scalar.Black);Assert.Throws<InvalidDataException>(()=>TemplateMatcher.Find(Bytes(frame),Bytes(flat),Asset(),96,.97));}
 [Fact]public void DpiChangeRequiresRelearning()
 {using var pattern=Pattern();using var frame=new Mat(180,240,MatType.CV_8UC1,Scalar.Black);Assert.Throws<InvalidOperationException>(()=>TemplateMatcher.Find(Bytes(frame),Bytes(pattern),Asset(),144,.97));}
 [Fact]public void SuccessProbeAllowsDifferentHeightWithoutPretendingItMatched()
 {using var pattern=Pattern();using var frame=new Mat(200,240,MatType.CV_8UC1,Scalar.Black);Assert.Null(TemplateMatcher.Find(Bytes(frame),Bytes(pattern),Asset(),96,.97,true,true));}
 [Theory][InlineData(360,260)][InlineData(200,160)]
 public void EditorExportStaysAnchoredToTopRightWhenWindowSizeChanges(int width,int height)
 {using var pattern=Pattern();using var frame=new Mat(height,width,MatType.CV_8UC1,Scalar.Black);var asset=Asset() with{Role="editor-export",Scene=Scene.Editor,Region=new(190,12,28,24),Padding=16};var x=width-50;using(var roi=new Mat(frame,new Rect(x,12,28,24)))pattern.CopyTo(roi);var hit=TemplateMatcher.Find(Bytes(frame),Bytes(pattern),asset,96,.97,anchorTopRight:true);Assert.NotNull(hit);Assert.Equal(x+14,hit.X);Assert.Equal(24,hit.Y);}
 [Fact]public void EditorTopRightSearchNeverFallsBackToIdenticalButtonInPageContent()
 {using var pattern=Pattern();using var frame=new Mat(260,360,MatType.CV_8UC1,Scalar.Black);using(var roi=new Mat(frame,new Rect(90,100,28,24)))pattern.CopyTo(roi);var asset=Asset() with{Role="editor-export",Scene=Scene.Editor,Region=new(190,12,28,24),Padding=16};Assert.Null(TemplateMatcher.Find(Bytes(frame),Bytes(pattern),asset,96,.97,anchorTopRight:true));}
 [Fact]public void TopRightMatchingStillRejectsDifferentDpi()
 {using var pattern=Pattern();using var frame=new Mat(260,360,MatType.CV_8UC1,Scalar.Black);Assert.Throws<InvalidOperationException>(()=>TemplateMatcher.Find(Bytes(frame),Bytes(pattern),Asset(),144,.97,anchorTopRight:true));}
 [Fact]public void OtherRolesStillRequireLearnedWindowSize()
 {using var pattern=Pattern();using var frame=new Mat(260,360,MatType.CV_8UC1,Scalar.Black);Assert.Throws<InvalidOperationException>(()=>TemplateMatcher.Find(Bytes(frame),Bytes(pattern),Asset(),96,.97));}

 [Fact]public void CompletionActionsCanMoveOutsideOldScaledSearchArea()
 {using var pattern=Pattern();using var frame=new Mat(280,400,MatType.CV_8UC1,Scalar.Black);using(var roi=new Mat(frame,new Rect(280,190,28,24)))pattern.CopyTo(roi);var asset=Asset() with{Role="completion-actions",Scene=Scene.Success};Assert.Null(TemplateMatcher.Find(Bytes(frame),Bytes(pattern),asset,96,.97,true,true));var hit=TemplateMatcher.Find(Bytes(frame),Bytes(pattern),asset,96,.97,allowResize:true,searchWholeWindow:true);Assert.NotNull(hit);Assert.Equal(294,hit.X);Assert.Equal(202,hit.Y);}
 [Fact]public void WholeCompletionDialogStillRejectsDuplicateMarkers()
 {using var pattern=Pattern();using var frame=new Mat(280,400,MatType.CV_8UC1,Scalar.Black);foreach(var point in new[]{new OpenCvSharp.Point(10,10),new OpenCvSharp.Point(280,190)}){using var roi=new Mat(frame,new Rect(point.X,point.Y,28,24));pattern.CopyTo(roi);}Assert.Throws<InvalidOperationException>(()=>TemplateMatcher.Find(Bytes(frame),Bytes(pattern),Asset(),96,.97,allowResize:true,searchWholeWindow:true));}

 [Theory][InlineData(400,280)][InlineData(200,160)]
 public void CompletionCloseFollowsDialogBottomRight(int width,int height)
 {using var pattern=Pattern();using var frame=new Mat(height,width,MatType.CV_8UC1,Scalar.Black);var asset=Asset() with{Role="completion-close",Scene=Scene.Success,Region=new(190,145,28,24),Padding=16};using(var roi=new Mat(frame,new Rect(width-50,height-35,28,24)))pattern.CopyTo(roi);var hit=TemplateMatcher.Find(Bytes(frame),Bytes(pattern),asset,96,.97,anchorBottomRight:true);Assert.NotNull(hit);Assert.Equal(width-36,hit.X);Assert.Equal(height-23,hit.Y);}
 [Fact]public void CompletionCloseSearchDoesNotClickMatchingWordInVideoPreview()
 {using var pattern=Pattern();using var frame=new Mat(280,400,MatType.CV_8UC1,Scalar.Black);using(var roi=new Mat(frame,new Rect(80,60,28,24)))pattern.CopyTo(roi);var asset=Asset() with{Role="completion-close",Scene=Scene.Success,Region=new(190,145,28,24),Padding=16};Assert.Null(TemplateMatcher.Find(Bytes(frame),Bytes(pattern),asset,96,.97,anchorBottomRight:true));}

 [Fact]public void WholeWindowScanFindsCloseOnMovedOrEmbeddedSharePanel()
 {using var pattern=Pattern();using var frame=new Mat(500,800,MatType.CV_8UC1,Scalar.Black);using(var roi=new Mat(frame,new Rect(490,300,28,24)))pattern.CopyTo(roi);var asset=Asset() with{Role="completion-close",Scene=Scene.Success,Region=new(190,145,28,24),Padding=16};Assert.Null(TemplateMatcher.Find(Bytes(frame),Bytes(pattern),asset,96,.97,anchorBottomRight:true));var hit=TemplateMatcher.Find(Bytes(frame),Bytes(pattern),asset,96,.97,allowResize:true,searchWholeWindow:true);Assert.NotNull(hit);Assert.Equal(504,hit.X);Assert.Equal(312,hit.Y);}

}
