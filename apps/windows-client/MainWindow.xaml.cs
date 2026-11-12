using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ChestTracker.Core;
using Forms = System.Windows.Forms;
namespace ChestTracker;

public partial class MainWindow : Window
{
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    private readonly ApiClient api = new();
    private readonly OverlayWindow overlay = new();
    private readonly OverlayToggle toggle = new();
    private readonly DispatcherTimer keys = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly DispatcherTimer sync = new() { Interval = TimeSpan.FromSeconds(5) };
    private readonly DispatcherTimer scan = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Forms.NotifyIcon tray = new();
    private readonly bool testMode;
    private Storyboard? animation;
    private Key hotkey = Key.O;
    private bool choosingKey, online, authenticated, monitoring, syncBusy, scanBusy, replaying, closing, overlayWanted = true;
    private string accountId = "", displayName = "", initialPassword = "";
    private GameWindow? game;
    private LiveWindowCapture? capture;
    private ChestDetector detector = new();
    private DateTimeOffset lastStats, nextCaptureAttempt;
    public MainWindow() : this(false) { }
    internal MainWindow(bool testMode)
    {
        this.testMode = testMode;
        InitializeComponent();
        var saved = testMode ? null : Storage.Load<Settings>("settings.dat");
        if (saved is not null) { ServerUrl.Text = saved.Server; Username.Text = saved.Username; EnableCapture.IsChecked = saved.LocalAnalysisConsent; if (Enum.TryParse<Key>(saved.Hotkey, out var key) && AllowedKey(key)) hotkey = key; }
        HotkeyButton.Content = $"{hotkey}  ·  Ändern";
        keys.Tick += (_, _) => { if (toggle.Update((GetAsyncKeyState(KeyInterop.VirtualKeyFromKey(hotkey)) & 0x8000) != 0, IsActive) && authenticated) ToggleOverlay(); };
        sync.Tick += async (_, _) => await Synchronize();
        scan.Tick += async (_, _) => await ScanGame();
        Loaded += (_, _) => { Username.Focus(); if (!testMode && SystemParameters.ClientAreaAnimation) { animation = (Storyboard)FindResource("BackdropMotion"); animation.Begin(this, true); } };
        Closing += (_, _) => { closing = true; keys.Stop(); sync.Stop(); scan.Stop(); capture?.Dispose(); overlay.Close(); tray.Dispose(); api.Dispose(); };
        if (testMode) return;
        keys.Start(); sync.Start(); scan.Start();
        tray.Icon = System.Drawing.SystemIcons.Application; tray.Text = "ChestTracker"; tray.Visible = true;
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("ChestTracker öffnen", null, (_, _) => Dispatcher.Invoke(() => { Show(); if (authenticated) WindowState = WindowState.Normal; Activate(); }));
        menu.Items.Add("Overlay ein / aus", null, (_, _) => Dispatcher.Invoke(ToggleOverlay));
        menu.Items.Add("Erfassung stoppen", null, (_, _) => Dispatcher.Invoke(async () => await StopMonitoring()));
        menu.Items.Add("Beenden", null, (_, _) => Dispatcher.Invoke(Close)); tray.ContextMenuStrip = menu;
    }
    private static bool AllowedKey(Key key) => key >= Key.A && key <= Key.Z || key >= Key.F1 && key <= Key.F12;
    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!choosingKey) return; e.Handled = true;
        if (e.Key == Key.Escape) { choosingKey = false; HotkeyButton.Content = $"{hotkey}  ·  Ändern"; return; }
        if (!AllowedKey(e.Key)) { HotkeyButton.Content = "Buchstabe oder F1–F12"; return; }
        hotkey = e.Key; choosingKey = false; toggle.Reset(); HotkeyButton.Content = $"{hotkey}  ·  Ändern"; SaveSettings();
    }
    private void Hotkey_Click(object sender, RoutedEventArgs e) { choosingKey = true; HotkeyButton.Content = "Taste drücken …"; }
    private void SaveSettings() => Storage.Save("settings.dat", new Settings(ServerUrl.Text.Trim(), Username.Text.Trim(), hotkey.ToString(), EnableCapture.IsChecked == true));
    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        if (LoginView.Visibility != Visibility.Visible || ChangePanel.Visibility == Visibility.Visible) return;
        LoginButton.IsEnabled = false; LoginStatus.Text = "";
        try
        {
            api.Configure(ServerUrl.Text); var user = await api.Login(Username.Text.Trim(), Password.Password);
            accountId = user.GetProperty("id").GetString()!; displayName = user.GetProperty("displayName").GetString()!;
            if (user.GetProperty("mustChangePassword").GetBoolean()) { initialPassword = Password.Password; ChangePanel.Visibility = Visibility.Visible; NewPassword.Focus(); }
            else await FinishLogin();
        }
        catch (Exception error) { LoginStatus.Text = UserError(error); }
        finally { LoginButton.IsEnabled = true; }
    }
    private async Task FinishLogin()
    {
        authenticated = true; online = true; initialPassword = ""; Password.Clear(); ChangePanel.Visibility = Visibility.Collapsed; SaveSettings();
        ShowAccount(displayName); api.RecoverIncompleteMatch();
        try { await RefreshStats(); } catch (Exception error) { ConnectionText.Text = UserError(error); }
        await Synchronize();
    }
    internal void ShowAccount(string name)
    {
        displayName = name; AccountName.Text = name; animation?.Stop(this); LoginView.Visibility = Visibility.Collapsed; AccountView.Visibility = Visibility.Visible;
        LoginButton.IsDefault = false; WindowState = WindowState.Normal; Width = 420; Height = 620; CenterWindow();
    }
    private void CenterWindow() { var area = Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle).WorkingArea; var scale = VisualTreeHelper.GetDpi(this); Left = (area.Left + area.Width / 2) / scale.DpiScaleX - Width / 2; Top = (area.Top + area.Height / 2) / scale.DpiScaleY - Height / 2; }
    private async void Change_Click(object sender, RoutedEventArgs e)
    {
        if (NewPassword.Password.Length < 12) { ChangeError.Text = "Bitte mindestens 12 Zeichen verwenden."; return; }
        if (NewPassword.Password != ConfirmPassword.Password) { ChangeError.Text = "Die Passwörter stimmen nicht überein."; return; }
        ChangeButton.IsEnabled = false;
        try { await api.Send("auth/password", new { currentPassword = initialPassword, newPassword = NewPassword.Password }); NewPassword.Clear(); ConfirmPassword.Clear(); await FinishLogin(); }
        catch (Exception error) { ChangeError.Text = UserError(error); }
        finally { ChangeButton.IsEnabled = true; }
    }
    private async void CancelChange_Click(object sender, RoutedEventArgs e) { try { await api.Logout(); } catch { } initialPassword = ""; Password.Clear(); ChangePanel.Visibility = Visibility.Collapsed; }
    private async void Check_Click(object sender, RoutedEventArgs e) { try { api.Configure(ServerUrl.Text); await api.Send("health", auth: false, get: true); LoginStatus.Text = "Server erreichbar."; } catch (Exception error) { LoginStatus.Text = UserError(error); } }
    private async Task RefreshStats()
    {
        var dashboard = await api.Send("dashboard", get: true);
        var user = dashboard.GetProperty("users").EnumerateArray().FirstOrDefault(u => u.GetProperty("id").GetString() == accountId);
        if (user.ValueKind == JsonValueKind.Undefined) return;
        var de = CultureInfo.GetCultureInfo("de-DE"); TotalChests.Text = user.GetProperty("total_chests").GetInt32().ToString("N0", de); TotalMatches.Text = user.GetProperty("completed_matches").GetInt32().ToString("N0", de); AverageChests.Text = user.GetProperty("average").GetDouble().ToString("0.#", de); lastStats = DateTimeOffset.UtcNow;
    }
    private async Task Synchronize()
    {
        if (testMode || closing || syncBusy || !authenticated) return; syncBusy = true;
        try
        {
            await api.Flush(); await api.Send("client/heartbeat", new { state = monitoring ? detector.State.ToString() : "UNKNOWN", matchId = detector.MatchId });
            if (DateTimeOffset.UtcNow - lastStats > TimeSpan.FromSeconds(10)) await RefreshStats();
            online = true; ConnectionText.Text = monitoring ? "Erfassung aktiv · Ereignisse werden synchronisiert." : "Mit deinem privaten Server verbunden.";
        }
        catch (Exception error) { online = false; ConnectionText.Text = UserError(error); if (error is ApiException a && a.Status == 401) { authenticated = false; await StopMonitoring(); LoginView.Visibility = Visibility.Visible; AccountView.Visibility = Visibility.Collapsed; LoginButton.IsDefault = true; WindowState = WindowState.Maximized; LoginStatus.Text = "Sitzung abgelaufen. Bitte erneut anmelden."; } }
        finally { syncBusy = false; ServerDot.Fill = Brush(online ? "#4FC48B" : "#727C8C"); ServerState.Text = online ? "Verbunden" : "Offline"; QueueText.Text = api.Pending == 0 ? "Alle Ereignisse synchronisiert" : $"{api.Pending} Ereignisse warten auf Verbindung"; UpdateOverlay(); }
    }
    private async void Game_Click(object sender, RoutedEventArgs e)
    {
        if (monitoring) { await StopMonitoring(); return; }
        if (replaying || !authenticated) return;
        monitoring = true; overlayWanted = true; detector = new("tesseract-ocr-experimental-v1", TimeSpan.FromSeconds(2)); GameButton.Content = "Fortnite stoppen"; GameStatus.Text = "Warte auf Fortnite …"; ConnectionText.Text = "Stoppen beendet die Erfassung, nicht das Spiel.";
        try { if (FortniteWindow.Find() is null) FortniteWindow.Launch(); } catch { ConnectionText.Text = "Epic Games Launcher nicht gefunden. Starte Fortnite manuell; das Fenster wird automatisch erkannt."; }
        await ScanGame();
    }
    private async Task ScanGame()
    {
        if (!monitoring || scanBusy || closing) return; scanBusy = true;
        try
        {
            var found = FortniteWindow.Find();
            if (found is null) { if (game is not null) { capture?.Dispose(); capture = null; EndIncomplete(); } game = null; GameStatus.Text = "Warte auf Fortnite …"; GameDot.Fill = Brush("#727C8C"); MatchStatus.Text = "Kein sichtbares Spielfenster"; overlay.Hide(); return; }
            bool changed = game?.Handle != found.Handle; game = found;
            if (EnableCapture.IsChecked == true && (changed || capture is null) && DateTimeOffset.UtcNow >= nextCaptureAttempt)
            {
                capture?.Dispose(); capture = new LiveWindowCapture(); var currentCapture = capture;
                capture.Observed += observation => { if (!closing) Dispatcher.InvokeAsync(() => { if (ReferenceEquals(capture, currentCapture)) ApplyObservation(observation, true); }); };
                capture.Diagnostic += message => { if (!closing) Dispatcher.InvokeAsync(() => DetectionStatus.Text = message); };
                capture.Start(found.Handle); DetectionStatus.Text = "Fensteraufnahme aktiv · OCR experimentell";
            }
            if (EnableCapture.IsChecked != true) { GameStatus.Text = "Fortnite erkannt"; GameDot.Fill = Brush("#727C8C"); MatchStatus.Text = "Bildanalyse ist ausgeschaltet"; }
            if (overlayWanted && !overlay.IsVisible) overlay.Show();
            UpdateOverlay();
        }
        catch (Exception error) { capture?.Dispose(); capture = null; nextCaptureAttempt = DateTimeOffset.UtcNow.AddSeconds(15); DetectionStatus.Text = "Bildanalyse nicht verfügbar: " + UserError(error); GameStatus.Text = "Fortnite erkannt · Status unbekannt"; }
        finally { scanBusy = false; }
        await Task.CompletedTask;
    }
    private void ApplyObservation(Observation observation, bool send)
    {
        if (closing || send && (!monitoring || !authenticated)) return; var update = detector.Process(observation);
        if (send) { if (update.Match is { } m) api.Enqueue(m.Status == "ACTIVE" ? "client/matches" : "client/matches/end", m.Status == "ACTIVE" ? (object)new { id = m.Id, startedAt = m.StartedAt } : new { id = m.Id, endedAt = m.EndedAt, status = m.Status }); if (update.Event is { } c) api.Enqueue("client/events", new { clientEventId = c.ClientEventId, matchId = c.MatchId, occurredAt = c.OccurredAt, confidence = c.Confidence, detectorVersion = c.DetectorVersion }); api.TrackMatch(update.MatchId, observation.At); }
        GameStatus.Text = update.State switch { GameState.IN_MATCH => "Im Match", GameState.LOBBY => "In der Lobby", GameState.LOADING => "Ladebildschirm", GameState.MATCH_END_PENDING => "Matchende wird geprüft", _ => "Fortnite erkannt · Status unbekannt" };
        GameDot.Fill = Brush(update.State == GameState.IN_MATCH ? "#5E9FF7" : update.State == GameState.LOBBY ? "#4FC48B" : "#727C8C");
        MatchStatus.Text = update.State == GameState.IN_MATCH ? $"Truhen in dieser Runde: {update.Count}" : update.State == GameState.LOBBY ? "Bereit für die nächste Runde" : "Warte auf eindeutige Bildhinweise"; UpdateOverlay();
    }
    private void EndIncomplete() { var m = detector.MarkIncomplete(DateTimeOffset.UtcNow); if (m is not null && api.Credentials is not null) api.Enqueue("client/matches/end", new { id = m.Id, endedAt = m.EndedAt, status = m.Status }); api.TrackMatch(null, DateTimeOffset.UtcNow); }
    private async Task StopMonitoring() { monitoring = false; capture?.Dispose(); capture = null; game = null; EndIncomplete(); GameButton.Content = "Fortnite starten"; GameStatus.Text = "Erfassung gestoppt"; MatchStatus.Text = "Kein laufendes Match"; GameDot.Fill = Brush("#727C8C"); overlay.Hide(); await Synchronize(); }
    private void CaptureSetting_Changed(object sender, RoutedEventArgs e) { if (!IsLoaded) return; SaveSettings(); if (EnableCapture.IsChecked != true) { capture?.Dispose(); capture = null; EndIncomplete(); DetectionStatus.Text = "Bildanalyse ausgeschaltet."; } else { nextCaptureAttempt = default; DetectionStatus.Text = "Experimentelle lokale Analyse freigegeben. Keine Bilder werden hochgeladen."; } }
    private void UpdateOverlay() { if (!authenticated || !monitoring || game is null) return; overlay.Update(displayName, detector.State, detector.Count, online, false, capture is not null); }
    private void ToggleOverlay() { if (!monitoring) return; overlayWanted = !overlayWanted; if (!overlayWanted) overlay.Hide(); else if (game is not null) { overlay.Show(); UpdateOverlay(); } }
    private async void Logout_Click(object sender, RoutedEventArgs e) { await StopMonitoring(); try { await api.Logout(); } catch { } authenticated = false; online = false; Password.Clear(); AccountView.Visibility = Visibility.Collapsed; LoginView.Visibility = Visibility.Visible; LoginButton.IsDefault = true; WindowState = WindowState.Maximized; LoginStatus.Text = ""; if (SystemParameters.ClientAreaAnimation) animation?.Begin(this, true); }
    private void Website_Click(object sender, RoutedEventArgs e) { try { Process.Start(new ProcessStartInfo(api.Server) { UseShellExecute = true }); } catch (Exception error) { ConnectionText.Text = UserError(error); } }
    private async void Replay_Click(object sender, RoutedEventArgs e)
    {
        if (monitoring || replaying) { ConnectionText.Text = "Bitte die Erfassung zuerst stoppen."; return; }
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Beobachtungs-Replay (*.json)|*.json" }; if (dialog.ShowDialog() != true) return;
        replaying = true; GameButton.IsEnabled = false;
        try { var frames = JsonSerializer.Deserialize<List<Observation>>(File.ReadAllText(dialog.FileName), new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } }) ?? []; detector = new(); foreach (var frame in frames) { if (closing) break; ApplyObservation(frame, false); await Task.Delay(83); } ConnectionText.Text = "Offline-Test beendet. Keine Testdaten hochgeladen."; }
        catch (Exception error) { ConnectionText.Text = UserError(error); }
        finally { detector = new(); replaying = false; GameButton.IsEnabled = true; }
    }
    private void Header_Drag(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left && e.OriginalSource is not System.Windows.Controls.Button) DragMove(); }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private static SolidColorBrush Brush(string value) => new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(value));
    private static string UserError(Exception error) => error is TaskCanceledException ? "Der Server antwortet nicht. Bitte Verbindung prüfen." : error.Message;
}
