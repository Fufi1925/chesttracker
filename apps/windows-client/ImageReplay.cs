using System.Drawing;
using System.Text.Json;
using ChestTracker.Core;
namespace ChestTracker;
public record Region(double X,double Y,double Width,double Height);
public record Cue(string Template,Region Region,double Threshold=.94);
public record ReplayFrame(string Image,double Seconds);
public record ImageManifest(Cue Chest,Cue OwnPlayer,Cue Match,Cue Lobby,Cue Loading,List<ReplayFrame> Frames);
/// <summary>Normalized grayscale template matching within explicitly configured HUD regions. Local recorded images only.</summary>
public static class ImageReplay
{
 public static List<Observation> Read(string manifestPath)
 {
   var root=Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
   var manifest=JsonSerializer.Deserialize<ImageManifest>(File.ReadAllText(manifestPath),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})??throw new Exception("Ungültiges Bildmanifest.");
   if(manifest.Frames.Count>36000)throw new Exception("Maximal 36.000 Bilder je Replay.");
   string Resolve(string relative){var p=Path.GetFullPath(Path.Combine(root,relative));if(!p.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Bildpfade müssen innerhalb des Replay-Ordners liegen.");return p;}
   using var chest=new Bitmap(Resolve(manifest.Chest.Template));using var own=new Bitmap(Resolve(manifest.OwnPlayer.Template));using var match=new Bitmap(Resolve(manifest.Match.Template));using var lobby=new Bitmap(Resolve(manifest.Lobby.Template));using var loading=new Bitmap(Resolve(manifest.Loading.Template));
   var at=DateTimeOffset.UtcNow;List<Observation> result=[];double previous=-1;
   foreach(var f in manifest.Frames)
   {
     if(f.Seconds<=previous)throw new Exception("Replay-Zeiten müssen streng aufsteigend sein.");previous=f.Seconds;
     using var frame=new Bitmap(Resolve(f.Image));
     if(frame.Width>7680||frame.Height>4320)throw new Exception("Bildauflösung zu groß.");
     var ms=Score(frame,match,manifest.Match.Region);var ls=Score(frame,lobby,manifest.Lobby.Region);var loadingScore=Score(frame,loading,manifest.Loading.Region);
     var state=GameState.UNKNOWN;var confidence=0d;
     if(ms>=manifest.Match.Threshold&&ls<manifest.Lobby.Threshold){state=GameState.IN_MATCH;confidence=ms;}
     else if(ls>=manifest.Lobby.Threshold&&ms<manifest.Match.Threshold){state=GameState.LOBBY;confidence=ls;}
     else if(loadingScore>=manifest.Loading.Threshold){state=GameState.LOADING;confidence=loadingScore;}
     result.Add(new(at.AddSeconds(f.Seconds),state,confidence,Score(frame,chest,manifest.Chest.Region),Score(frame,own,manifest.OwnPlayer.Region)));
   }
   return result;
 }
 public static double Score(Bitmap source,Bitmap template,Region region)
 {
   if(region.X<0||region.Y<0||region.Width<=0||region.Height<=0||region.X+region.Width>1||region.Y+region.Height>1)throw new Exception("HUD-Region muss innerhalb des Bildes liegen (0 bis 1).");
   var crop=new Rectangle((int)(source.Width*region.X),(int)(source.Height*region.Y),Math.Max(1,(int)(source.Width*region.Width)),Math.Max(1,(int)(source.Height*region.Height)));
   // Normalize ROI to template size. Calibration must tightly bound each cue.
   using var normalized=new Bitmap(template.Width,template.Height);using(var g=Graphics.FromImage(normalized))g.DrawImage(source,new Rectangle(0,0,template.Width,template.Height),crop,GraphicsUnit.Pixel);
   int n=template.Width*template.Height;if(n>1000000)throw new Exception("Template zu groß.");
   double sumA=0,sumB=0,sumAA=0,sumBB=0,sumAB=0;
   for(int y=0;y<template.Height;y++)for(int x=0;x<template.Width;x++){var a=normalized.GetPixel(x,y);var b=template.GetPixel(x,y);double av=.299*a.R+.587*a.G+.114*a.B,bv=.299*b.R+.587*b.G+.114*b.B;sumA+=av;sumB+=bv;sumAA+=av*av;sumBB+=bv*bv;sumAB+=av*bv;}
   var denominator=Math.Sqrt((sumAA-sumA*sumA/n)*(sumBB-sumB*sumB/n));return denominator<=0?0:Math.Clamp((sumAB-sumA*sumB/n)/denominator,0,1);
 }
}
