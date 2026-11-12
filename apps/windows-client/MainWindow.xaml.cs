using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using ChestTracker.Core;
using Microsoft.Win32;
using Forms=System.Windows.Forms;
namespace ChestTracker;
public partial class MainWindow : Window
{
 [DllImport("user32.dll")]static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
 [DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr h,int id);
 private readonly OverlayWindow overlay=new();private readonly ApiClient api=new();private ChestDetector detector=new();
 private readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromSeconds(5)};private readonly Forms.NotifyIcon tray=new();
 private bool busy,paused,running,online;private IntPtr handle;private string name="ChestTracker";private CancellationTokenSource? replayCancel;
 public MainWindow()
 {
  InitializeComponent();var settings=Storage.Load<Settings>("settings.dat");if(settings is not null){ServerUrl.Text=settings.Server;Username.Text=settings.Username;Hotkey.SelectedIndex=settings.Hotkey=="F8"?1:settings.Hotkey=="F9"?2:0;}
  SourceInitialized+=(_,_)=>{handle=new WindowInteropHelper(this).Handle;HwndSource.FromHwnd(handle).AddHook(WndProc);SetHotkey();};
  Loaded+=async(_,_)=>{overlay.Show();try{api.Configure(ServerUrl.Text);if(api.Credentials is not null){name=api.Credentials.DisplayName;var user=await api.Send("auth/me",get:true);if(user.GetProperty("user").GetProperty("mustChangePassword").GetBoolean())ChangePanel.Visibility=Visibility.Visible;else api.RecoverIncompleteMatch();online=true;ConnectionText.Text=$"Angemeldet als {name}";}}catch(Exception e){ConnectionText.Text=e.Message;}Update();};
  timer.Tick+=async(_,_)=>await Sync();timer.Start();
  tray.Icon=System.Drawing.SystemIcons.Application;tray.Text="ChestTracker";tray.Visible=true;
  var menu=new Forms.ContextMenuStrip();menu.Items.Add("Overlay anzeigen / ausblenden",null,(_,_)=>Dispatcher.Invoke(Toggle));menu.Items.Add("Erkennung pausieren / fortsetzen",null,(_,_)=>Dispatcher.Invoke(()=>paused=!paused));menu.Items.Add("Einstellungen",null,(_,_)=>Dispatcher.Invoke(()=>{Show();Activate();}));menu.Items.Add("Website öffnen",null,(_,_)=>Dispatcher.Invoke(OpenWebsite));menu.Items.Add("Beenden",null,(_,_)=>Dispatcher.Invoke(Close));tray.ContextMenuStrip=menu;
  Closing+=(_,_)=>{replayCancel?.Cancel();timer.Stop();UnregisterHotKey(handle,1);overlay.Close();tray.Dispose();api.Dispose();};
 }
 private IntPtr WndProc(IntPtr hwnd,int msg,IntPtr w,IntPtr l,ref bool handled){if(msg==0x0312&&w.ToInt32()==1){Toggle();handled=true;}return IntPtr.Zero;}
 private void SetHotkey(){if(handle==IntPtr.Zero)return;UnregisterHotKey(handle,1);var key=Hotkey.SelectedIndex==1?0x77u:Hotkey.SelectedIndex==2?0x78u:0x4Fu;if(!RegisterHotKey(handle,1,0x4000,key))ConnectionText.Text="Hotkey bereits belegt. Bitte O, F8 oder F9 auswählen.";}
 private void Hotkey_Changed(object sender,SelectionChangedEventArgs e){if(IsLoaded)SetHotkey();}
 private void Toggle(){if(overlay.IsVisible)overlay.Hide();else overlay.Show();}
 private void Toggle_Click(object sender,RoutedEventArgs e)=>Toggle();
 private void Pause_Click(object sender,RoutedEventArgs e){paused=!paused;DetectorText.Text=paused?"Replay pausiert":"Replay fortgesetzt";}
 private void OpenWebsite(){try{var uri=new Uri(ServerUrl.Text);if(uri.Scheme is not ("http" or "https"))throw new Exception("Ungültige Website-Adresse.");Process.Start(new ProcessStartInfo(uri.AbsoluteUri){UseShellExecute=true});}catch(Exception e){ConnectionText.Text=e.Message;}}
 private void Website_Click(object sender,RoutedEventArgs e)=>OpenWebsite();
 private void Save_Click(object sender,RoutedEventArgs e){try{api.Configure(ServerUrl.Text);SaveSettings();ConnectionText.Text="Einstellungen sicher gespeichert.";}catch(Exception ex){ConnectionText.Text=ex.Message;}}
 private void SaveSettings()=>Storage.Save("settings.dat",new Settings(ServerUrl.Text,Username.Text,Hotkey.SelectedIndex==1?"F8":Hotkey.SelectedIndex==2?"F9":"O"));
 private async void Check_Click(object sender,RoutedEventArgs e){try{api.Configure(ServerUrl.Text);await api.Send("health",auth:false,get:true);ConnectionText.Text="Server erreichbar.";}catch(Exception ex){ConnectionText.Text=ex.Message;}}
 private async void Login_Click(object sender,RoutedEventArgs e){LoginButton.IsEnabled=false;try{api.Configure(ServerUrl.Text);var user=await api.Login(Username.Text,Password.Password);name=user.GetProperty("displayName").GetString()!;ChangePanel.Visibility=user.GetProperty("mustChangePassword").GetBoolean()?Visibility.Visible:Visibility.Collapsed;if(ChangePanel.Visibility==Visibility.Collapsed)Password.Clear();online=true;ConnectionText.Text=$"Angemeldet als {name}";SaveSettings();}catch(Exception ex){ConnectionText.Text=ex.Message;}finally{LoginButton.IsEnabled=true;Update();}}
 private async void Change_Click(object sender,RoutedEventArgs e){try{await api.Send("auth/password",new{currentPassword=Password.Password,newPassword=NewPassword.Password});Password.Clear();NewPassword.Clear();ChangePanel.Visibility=Visibility.Collapsed;ConnectionText.Text="Passwort geändert.";}catch(Exception ex){ConnectionText.Text=ex.Message;}}
 private async void Logout_Click(object sender,RoutedEventArgs e){replayCancel?.Cancel();try{await api.Logout();}catch(Exception ex){ConnectionText.Text=ex.Message;}online=false;name="ChestTracker";Password.Clear();Update();}
 private async Task Sync()
 {
  if(busy||api.Credentials is null||ChangePanel.Visibility==Visibility.Visible)return;busy=true;
  try{await api.Flush();await api.Send("client/heartbeat",new{state=SyncReplay.IsChecked==true?detector.State.ToString():"UNKNOWN",matchId=SyncReplay.IsChecked==true?detector.MatchId:null});online=true;ConnectionText.Text=$"Verbunden als {api.Credentials.DisplayName}";}
  catch(Exception e){online=false;ConnectionText.Text=e is ApiException a&&a.Status is 401 or 428?e.Message:"Server offline oder Anfrage abgelehnt: "+e.Message;}
  finally{busy=false;Update();}
 }
 private void Update(){overlay.Update(name,detector.State,detector.Count,online,true);QueueText.Text=$"Warteschlange: {api.Pending}";}
 private async void Replay_Click(object sender,RoutedEventArgs e)
 {
  var dialog=new Microsoft.Win32.OpenFileDialog{Filter="Beobachtungs-Replay (*.json)|*.json"};if(dialog.ShowDialog()!=true)return;
  try{var frames=JsonSerializer.Deserialize<List<Observation>>(File.ReadAllText(dialog.FileName),new JsonSerializerOptions{PropertyNameCaseInsensitive=true,Converters={new System.Text.Json.Serialization.JsonStringEnumConverter()}})??[];if(frames.Count==0)throw new Exception("Replay enthält keine Frames.");var first=frames[0].At;var now=DateTimeOffset.UtcNow;await RunReplay(frames.Select(f=>f with{At=now+(f.At-first)}).ToList());}catch(Exception ex){DetectorText.Text=ex.Message;}
 }
 private async void ImageReplay_Click(object sender,RoutedEventArgs e)
 {
  var dialog=new Microsoft.Win32.OpenFileDialog{Filter="Bildmanifest (*.json)|*.json"};if(dialog.ShowDialog()!=true)return;
  try{DetectorText.Text="Bilder werden lokal ausgewertet …";var frames=await Task.Run(()=>ImageReplay.Read(dialog.FileName));await RunReplay(frames);}catch(Exception ex){DetectorText.Text=ex.Message;}
 }
 private async Task RunReplay(List<Observation> frames)
 {
  if(running)throw new Exception("Bitte das laufende Replay zuerst beenden.");
  if(SyncReplay.IsChecked==true&&api.Credentials is null)throw new Exception("Für die Synchronisierung bitte zuerst anmelden.");
  var send=SyncReplay.IsChecked==true;if(send&&System.Windows.MessageBox.Show("Dieses Replay erzeugt echte Testdatensätze in eurer Statistik. Fortfahren?","Testdaten synchronisieren",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
  if(frames.Count==0)throw new Exception("Replay enthält keine Bilder.");
  detector=new();running=true;replayCancel=new();var token=replayCancel.Token;SyncReplay.IsEnabled=false;
  var replayClock=Stopwatch.StartNew();var firstFrame=frames[0].At;
  try{
   foreach(var frame in frames){token.ThrowIfCancellationRequested();var due=frame.At-firstFrame-replayClock.Elapsed;if(due>TimeSpan.Zero)await Task.Delay(due,token);while(paused){await Task.Delay(100,token);}var update=detector.Process(frame);
    if(send){if(update.Match is { } m)api.Enqueue(m.Status=="ACTIVE"?"client/matches":"client/matches/end",m.Status=="ACTIVE"?(object)new{id=m.Id,startedAt=m.StartedAt}:new{id=m.Id,endedAt=m.EndedAt,status=m.Status});if(update.Event is { } chest)api.Enqueue("client/events",new{clientEventId=chest.ClientEventId,matchId=chest.MatchId,occurredAt=chest.OccurredAt,confidence=chest.Confidence,detectorVersion=chest.DetectorVersion});}
    if(send)api.TrackMatch(update.MatchId,frame.At);DetectorText.Text=$"Offline-Replay · {update.State} · Truhen: {update.Count}";Update();
   }
  }catch(OperationCanceledException){DetectorText.Text="Replay abgebrochen.";}
  finally{var incomplete=detector.MarkIncomplete(DateTimeOffset.UtcNow);if(send&&incomplete is not null)api.Enqueue("client/matches/end",new{id=incomplete.Id,endedAt=incomplete.EndedAt,status=incomplete.Status});if(send)api.TrackMatch(null,DateTimeOffset.UtcNow);running=false;SyncReplay.IsEnabled=true;Update();if(send)await Sync();}
 }
}
