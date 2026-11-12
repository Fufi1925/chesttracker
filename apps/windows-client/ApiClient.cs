using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
namespace ChestTracker;

public sealed class ApiClient : IDisposable
{
    private readonly HttpClient http;
    private readonly SemaphoreSlim refreshLock = new(1, 1);
    public ApiClient(HttpMessageHandler? handler = null) { http = handler is null ? new HttpClient() : new HttpClient(handler); http.Timeout = TimeSpan.FromSeconds(20); http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); }
    public Credentials? Credentials { get; private set; }
    public string Server { get; private set; } = "";
    private readonly List<PendingCommand> queue = [];
    private string? queueOwner;
    public int Pending => queue.Count;
    public void Configure(string server) { var uri = new Uri(server.Trim().TrimEnd('/') + "/"); if (uri.Scheme != "https" && !(uri.IsLoopback && uri.Scheme == "http")) throw new Exception("Die API-Adresse muss HTTPS verwenden."); if (uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) throw new Exception("Bitte nur die Website-Basisadresse ohne Pfad eingeben."); if (Server != "" && Server != uri.AbsoluteUri && Pending > 0) throw new Exception("Zuerst die Warteschlange am bisherigen Server synchronisieren."); Server = uri.AbsoluteUri; Credentials = Storage.Load<Credentials>(Scope("session")); queue.Clear(); var saved = Storage.Load<PendingQueue>(Scope("queue")); queueOwner = saved?.OwnerId; queue.AddRange(saved?.Commands ?? []); }
    private string Scope(string type) => type + "-" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Server)))[..16] + ".dat";
    public async Task<JsonElement> Login(string username, string password) { var r = await Send("auth/login", new { username, password, client = "desktop" }, false); var u = r.GetProperty("user"); if (queueOwner is not null && queueOwner != u.GetProperty("id").GetString() && Pending > 0) throw new Exception("Es warten Ereignisse des bisherigen Kontos. Bitte zuerst damit synchronisieren."); SetCredentials(new(u.GetProperty("id").GetString()!, u.GetProperty("displayName").GetString()!, r.GetProperty("accessToken").GetString()!, r.GetProperty("refreshToken").GetString()!, DateTimeOffset.UtcNow.AddSeconds(r.GetProperty("expiresIn").GetInt32()))); return u; }
    private void SetCredentials(Credentials c) { Credentials = c; Storage.Save(Scope("session"), c); }
    private async Task EnsureToken() { if (Credentials is null) throw new Exception("Bitte zuerst anmelden."); if (Credentials.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(45)) return; await refreshLock.WaitAsync(); try { if (Credentials is null) throw new Exception("Bitte zuerst anmelden."); if (Credentials.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(45)) return; var r = await Send("auth/refresh", new { refreshToken = Credentials.RefreshToken }, false); SetCredentials(Credentials with { AccessToken = r.GetProperty("accessToken").GetString()!, RefreshToken = r.GetProperty("refreshToken").GetString()!, ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(r.GetProperty("expiresIn").GetInt32()) }); } finally { refreshLock.Release(); } }
    public async Task<JsonElement> Send(string path, object? body = null, bool auth = true, bool get = false) { if (auth) await EnsureToken(); using var req = new HttpRequestMessage(get ? HttpMethod.Get : HttpMethod.Post, Server + "api/" + path); if (auth) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Credentials!.AccessToken); else if (path == "auth/login") req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "desktop-login"); else if (path == "auth/refresh") req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "desktop-refresh"); if (!get) req.Content = JsonContent.Create(body ?? new { }); using var response = await http.SendAsync(req); JsonElement result; try { result = await response.Content.ReadFromJsonAsync<JsonElement>(); } catch (JsonException) { throw new ApiException((int)response.StatusCode, $"Keine gültige API-Antwort (HTTP {(int)response.StatusCode}). Bitte API-Adresse prüfen; möglicherweise wird eine Anmeldeseite vorgeschaltet."); } if (!response.IsSuccessStatusCode) throw new ApiException((int)response.StatusCode, result.TryGetProperty("error", out var e) ? e.GetString()! : "Serveranfrage fehlgeschlagen."); return result; }
    public void Enqueue(string path, object body) { if (Credentials is null) throw new Exception("Bitte zuerst anmelden."); if (queueOwner is not null && queueOwner != Credentials.UserId && Pending > 0) throw new Exception("Die Warteschlange gehört zu einem anderen Konto."); queueOwner = Credentials.UserId; queue.Add(new(path, JsonSerializer.SerializeToElement(body))); SaveQueue(); }
    private void SaveQueue() => Storage.Save(Scope("queue"), new PendingQueue(queueOwner ?? "", queue));
    public void TrackMatch(Guid? id, DateTimeOffset at) { if (Credentials is null) return; var key = Scope("recovery-" + Credentials.UserId); if (id is null) Storage.Delete(key); else Storage.Save(key, new RecoveryMatch(id.Value, at)); }
    public void RecoverIncompleteMatch() { if (Credentials is null) return; var key = Scope("recovery-" + Credentials.UserId); var match = Storage.Load<RecoveryMatch>(key); if (match is null) return; Enqueue("client/matches/end", new { id = match.Id, endedAt = match.LastObservedAt, status = "INCOMPLETE" }); Storage.Delete(key); }
    public async Task Flush() { if (Credentials is null || Pending > 0 && queueOwner != Credentials.UserId) throw new Exception("Die Warteschlange gehört zu einem anderen Konto."); while (queue.Count > 0) { await Send(queue[0].Path, queue[0].Body); queue.RemoveAt(0); SaveQueue(); } }
    public async Task Logout() { try { if (Credentials is not null) await Send("auth/logout", new { }); } finally { Credentials = null; Storage.Delete(Scope("session")); } }
    public void Dispose() => http.Dispose();
}
public sealed class ApiException(int status, string message) : Exception(message) { public int Status => status; }
