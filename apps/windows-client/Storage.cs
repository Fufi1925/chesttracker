using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace ChestTracker;
public static class Storage
{
 private static string Root=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ChestTracker");
 public static void Save<T>(string name,T data){Directory.CreateDirectory(Root);var bytes=ProtectedData.Protect(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data)),null,DataProtectionScope.CurrentUser);var path=Path.Combine(Root,name);File.WriteAllBytes(path+".tmp",bytes);File.Move(path+".tmp",path,true);}
 public static T? Load<T>(string name){var p=Path.Combine(Root,name);if(!File.Exists(p))return default;try{return JsonSerializer.Deserialize<T>(ProtectedData.Unprotect(File.ReadAllBytes(p),null,DataProtectionScope.CurrentUser));}catch(CryptographicException){return default;}}
 public static void Delete(string name){var p=Path.Combine(Root,name);if(File.Exists(p))File.Delete(p);}
}
public record Settings(string Server,string Username,string Hotkey);
public record Credentials(string UserId,string DisplayName,string AccessToken,string RefreshToken,DateTimeOffset ExpiresAt);
public record PendingCommand(string Path,JsonElement Body);
public record PendingQueue(string OwnerId,List<PendingCommand> Commands);
public record RecoveryMatch(Guid Id,DateTimeOffset LastObservedAt);
