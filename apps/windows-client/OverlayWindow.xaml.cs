using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using ChestTracker.Core;
namespace ChestTracker;
public partial class OverlayWindow : Window
{
 [DllImport("user32.dll",EntryPoint="GetWindowLongW")] private static extern int GetWindowLong(IntPtr h,int index);
 [DllImport("user32.dll",EntryPoint="SetWindowLongW")] private static extern int SetWindowLong(IntPtr h,int index,int value);
 public OverlayWindow(){InitializeComponent();SourceInitialized+=(_,_)=>{var h=new WindowInteropHelper(this).Handle;SetWindowLong(h,-20,GetWindowLong(h,-20)|0x20|0x80000|0x08000000);};Loaded+=(_,_)=>{Left=SystemParameters.WorkArea.Right-Width-12;Top=SystemParameters.WorkArea.Top+12;};}
 public void Update(string name,GameState state,int count,bool online,bool replay,bool captureActive=false,string? analysisHint=null)
 {
   var color=!online?"#7E8795":state==GameState.IN_MATCH?"#5E9FF7":state==GameState.LOBBY?"#4FC48B":"#7E8795";
   Dot.Fill=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
   var text=!online?"Server offline":analysisHint is not null?analysisHint:state==GameState.IN_MATCH?$"Truhen: {count}":state==GameState.LOBBY?"Lobby":state==GameState.LOADING?"Lädt":state==GameState.MATCH_END_PENDING?$"Rundenende · {count} Truhen":!captureActive&&!replay?"Bildanalyse aus":"HUD unbekannt";
   Label.Text=$"{name} | {text}";
   ToolTip=replay?"Offline-Replay":captureActive?"Lokale Bilderkennung · Referenzversion 2":"Fortnite-Fenster erkannt · Bildanalyse ausgeschaltet";
 }
}
