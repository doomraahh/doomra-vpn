using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
namespace Luna {
 // Explicit operator tool using the production worker and core lifecycle.
 static class NetworkProbe {
  static readonly string report=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"network-probe-safe.txt");
  static void Log(string text) { lock(report) File.AppendAllText(report,text+"\r\n"); }
  static bool Tun() {return NetworkInterface.GetAllNetworkInterfaces().Any(n=>n.Name==Configuration.InterfaceName && n.OperationalStatus==OperationalStatus.Up);}
  [STAThread] static int Main(string[] args) {
   if(args.Length==3 && args[0]=="--worker") {int pid;return int.TryParse(args[2],out pid)?Worker.Run(args[1],pid):1;}
   if(args.Length!=2 || (args[0]!="--check-profile" && args[0]!="--check-xray-http" && args[0]!="--check-singbox-http" && args[0]!="--i-accept-network-change")) return 2;
   ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
   File.WriteAllText(report,"Safe network verification; no credentials or raw core output.\r\n");
   try {
    byte[] plain=ProtectedData.Unprotect(File.ReadAllBytes(args[1]),null,DataProtectionScope.CurrentUser); ImportResult result;
    try {result=Subscriptions.Parse(System.Text.Encoding.UTF8.GetString(plain));} finally {Array.Clear(plain,0,plain.Length);}
    Log("Imported="+result.Servers.Count+"; rejected="+result.Errors.Count);
    if(result.Errors.Count!=0 || result.Servers.Count==0) return 1;
    if(args[0]=="--check-profile") return Check(result).GetAwaiter().GetResult();
    if(args[0]=="--check-xray-http" || args[0]=="--check-singbox-http") return CheckHttp(result.Servers[0],args[0]=="--check-singbox-http").GetAwaiter().GetResult();
    var target=result.Servers.Single(s=>s.Host=="194.26.192.151" && s.Port==443 && s.Protocol=="vless" && s.Security=="reality" && s.Flow=="xtls-rprx-vision" && s.Sni=="www.microsoft.com" && s.Fingerprint=="chrome");
    return Run(target).GetAwaiter().GetResult();
   } catch {Log("Verification failed; sensitive details suppressed.");return 1;}
  }
  static async Task<int> CheckHttp(Server server,bool singbox) {
   var listener=new System.Net.Sockets.TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
   var config=Json.Read<System.Collections.Generic.Dictionary<string,object>>(XrayConfig.Build(server,port,"probe","synthetic-probe"));
   config["log"]=Configuration.Obj("loglevel","info");
   config["inbounds"]=new object[]{Configuration.Obj("listen","127.0.0.1","port",port,"protocol","http","settings",Configuration.Obj("accounts",new object[]{Configuration.Obj("user","probe","pass","synthetic-probe")}))};
   string json=singbox?ServerCheck.Build(server,port,Configuration.BuildOutbound(server),new NetworkCredential("probe","synthetic-probe")):Json.Write(config);
   string singboxPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"sing-box.exe");
   if(singbox) Configuration.Check(singboxPath,json);else XrayBridge.Check(json);
   var info=singbox?Configuration.StartInfo(singboxPath,"run"):new System.Diagnostics.ProcessStartInfo(XrayBridge.CorePath,"run -format json -config stdin:") {UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
   using(var core=new System.Diagnostics.Process {StartInfo=info}) {
    Action<string> observe=line=> {if(line==null) return;foreach(string category in new[]{"accepted","failed","reality","timeout","refused","authentication","invalid","connectex","received real certificate","invalid connection","failed to read","handshake","certificate","connection reset","EOF","dial tcp","failed to verify","MLKEM","public key","short id"}) if(line.IndexOf(category,StringComparison.OrdinalIgnoreCase)>=0) Log("Xray event: "+category);};
    core.OutputDataReceived+=(o,e)=>observe(e.Data);core.ErrorDataReceived+=(o,e)=>observe(e.Data);
    core.Start();using(var job=Native.GuardChild(core)) {
     core.BeginOutputReadLine();core.BeginErrorReadLine();using(var input=core.StandardInput) input.Write(json);
     await Task.Delay(750);
     try {
      using(var handler=new HttpClientHandler {UseProxy=true,Proxy=new WebProxy("http://127.0.0.1:"+port) {Credentials=new NetworkCredential("probe","synthetic-probe")},UseCookies=false})
      using(var http=new HttpClient(handler) {Timeout=TimeSpan.FromSeconds(20)}) {
       string ip=(await http.GetStringAsync("https://api4.ipify.org")).Trim();bool ok=ip=="194.26.192.151";Log("Direct Xray HTTPS target IPv4="+ok);return ok?0:1;
      }
     } catch(Exception e) {Log("Direct Xray HTTPS failed: "+e.GetType().Name);return 1;}
     finally {if(!core.HasExited) {core.Kill();core.WaitForExit(3000);}}
    }
   }
  }
  static async Task<int> Check(ImportResult imported) {
   int failures=0,index=0;
   foreach(var server in imported.Servers) {
    index++;
    try {
     bool bridge=XrayBridge.Required(server);
     if(bridge) XrayBridge.Check(XrayConfig.Build(server,10808,"validation","synthetic-validation"));
     Configuration.Check(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"sing-box.exe"),Configuration.Build(server,bridge?XrayBridge.Socks(10808,"validation","synthetic-validation"):null));
     Log("Config "+index+": accepted; bridge="+bridge);
     if(imported.Servers.Count==1 || System.Text.RegularExpressions.Regex.IsMatch(server.Name,"каскад|cascade",System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
      int? latency=await ServerCheck.Run(server,CancellationToken.None);
      Log("HTTPS through proxy "+index+": "+(latency.HasValue?"PASS":"FAIL"));if(!latency.HasValue) failures++;
     }
    } catch {failures++;Log("Config "+index+": failed; details hidden.");}
   }
   return failures==0?0:1;
  }
  static async Task<bool> Dns() {
   try {var task=System.Net.Dns.GetHostAddressesAsync("www.microsoft.com");return await Task.WhenAny(task,Task.Delay(10000))==task && (await task).Length>0;} catch {return false;}
  }
  static async Task<string> Ip() {
   using(var handler=new HttpClientHandler {UseProxy=false,UseCookies=false,AllowAutoRedirect=false})
   using(var http=new HttpClient(handler) {Timeout=TimeSpan.FromSeconds(15)}) {
    string text=(await http.GetStringAsync("https://api4.ipify.org")).Trim();IPAddress address;
    if(!IPAddress.TryParse(text,out address) || address.AddressFamily!=System.Net.Sockets.AddressFamily.InterNetwork) throw new Exception();return text;
   }
  }
  static async Task<int> Run(Server server) {
   if(Tun()) {Log("Existing VPN TUN detected; test skipped.");return 1;}
   bool beforeDns=await Dns();string before=await Ip();Log("Before: DNS="+beforeDns+"; HTTPS IPv4=PASS");
   bool tunnel=false,dns=false;
   using(var connection=new Connection(false)) {
    var ready=new TaskCompletionSource<bool>();
    connection.Status+=state=> {string category=CoreFailure.Category(state);Log(category+CoreFailure.ExitSuffix(state));if(category=="CONNECTED") ready.TrySetResult(true);else if(category.StartsWith("ERROR")) ready.TrySetResult(false);};
    Task session=connection.Start(server,false,new NetworkPolicy {Mode="all",KillSwitch=false});
    try {
     var done=await Task.WhenAny(ready.Task,session,Task.Delay(65000));
     if(done==ready.Task && await ready.Task) {
      string actual=await Ip();tunnel=actual=="194.26.192.151";Log("Tunnel HTTPS IPv4="+(tunnel?"194.26.192.151 PASS":"FAIL"));
      dns=await Dns();Log("Tunnel system DNS="+dns+"; configured DNS=DoH via proxy");
     } else Log("Tunnel readiness failed or timed out.");
    } catch {Log("Traffic verification failed; details hidden.");}
    finally {connection.Stop();}
    if(await Task.WhenAny(session,Task.Delay(35000))!=session) Log("Worker cleanup timed out.");
    else {try {await session;} catch {Log("Session failure; details hidden.");}}
   }
   for(int i=0;i<20 && Tun();i++) await Task.Delay(250);
   bool afterDns=await Dns();string after=await Ip();bool recovered=!Tun() && afterDns && after==before;
   Log("After: TUN removed="+!Tun()+"; DNS="+afterDns+"; HTTPS original IPv4 restored="+(after==before));
   Log("Result="+(tunnel && dns && recovered?"PASS":"FAIL"));return tunnel && dns && recovered?0:1;
  }
 }
}
