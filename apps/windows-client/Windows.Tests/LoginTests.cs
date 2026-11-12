using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
namespace ChestTracker.Tests;
public sealed class LoginTests
{
 private static string Endpoint()=>"https://test-"+Guid.NewGuid().ToString("N")+".example/";
 private static HttpResponseMessage Json(object data,HttpStatusCode status=HttpStatusCode.OK)=>new(status){Content=new StringContent(JsonSerializer.Serialize(data),Encoding.UTF8,"application/json")};
 private sealed class Handler(Func<HttpRequestMessage,Task<HttpResponseMessage>> work):HttpMessageHandler
 {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>work(request);}
 [Fact]public async Task LoginPreservesBothCasesOfOAndUsesDesktopProtocol()
 {
  var calls=0;
  using var api=new ApiClient(new Handler(async req=>{
   calls++;
   if(req.RequestUri!.AbsolutePath=="/api/auth/login"){
    Assert.Equal("desktop-login",req.Headers.Authorization!.Parameter);
    var body=JsonDocument.Parse(await req.Content!.ReadAsStringAsync()).RootElement;
    Assert.Equal("Johan",body.GetProperty("username").GetString());Assert.Equal("new-Oo-password-2026",body.GetProperty("password").GetString());Assert.Equal("desktop",body.GetProperty("client").GetString());
    return Json(new{user=new{id="test-user",displayName="Johan",mustChangePassword=false},accessToken="test-access",refreshToken="test-refresh",expiresIn=900});
   }
   Assert.Equal("Bearer",req.Headers.Authorization!.Scheme);Assert.Equal("test-access",req.Headers.Authorization.Parameter);return Json(new{user=new{id="test-user"}});
  }));
  api.Configure(Endpoint());await api.Login("Johan","new-Oo-password-2026");await api.Send("auth/me",get:true);Assert.Equal(2,calls);
 }
 [Fact]public async Task HtmlLoginPageProducesReadableError()
 {
  using var api=new ApiClient(new Handler(_=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("<html>SSO</html>",Encoding.UTF8,"text/html")})));
  api.Configure(Endpoint());var error=await Assert.ThrowsAsync<ApiException>(()=>api.Login("Johan","test-password"));Assert.Contains("API-Adresse",error.Message);
 }
 [Fact]public async Task ServerCredentialErrorIsShownWithoutDiscardingIt()
 {
  using var api=new ApiClient(new Handler(_=>Task.FromResult(Json(new{error="Benutzername oder Passwort ist falsch."},HttpStatusCode.Unauthorized))));
  api.Configure(Endpoint());var error=await Assert.ThrowsAsync<ApiException>(()=>api.Login("Johan","wrong"));Assert.Equal(401,error.Status);Assert.Equal("Benutzername oder Passwort ist falsch.",error.Message);
 }
}
