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
}
