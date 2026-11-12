using System.IO;
using System.Drawing;
using Xunit;
namespace ChestTracker.Tests;
public sealed class PixelTests
{
 [Fact]public void ExactOwnTemplateMatchesButInvertedCueDoesNot()
 {
  using var a=new Bitmap(16,16);using var b=new Bitmap(16,16);using var wrong=new Bitmap(16,16);
  for(int y=0;y<16;y++)for(int x=0;x<16;x++){var value=(x*17+y*13)%256;var c=Color.FromArgb(value,value,value);a.SetPixel(x,y,c);b.SetPixel(x,y,c);wrong.SetPixel(x,y,Color.FromArgb(255-value,255-value,255-value));}
  Assert.True(ImageReplay.Score(a,b,new(0,0,1,1))>.99);Assert.True(ImageReplay.Score(a,wrong,new(0,0,1,1))<.5);
 }
 [Fact]public void OutOfBoundsRegionFails(){using var b=new Bitmap(16,16);Assert.Throws<Exception>(()=>ImageReplay.Score(b,b,new(.9,0,.5,1)));}
}
