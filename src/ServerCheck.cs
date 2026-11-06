using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace Luna {
 internal static class ServerCheck {
  internal static string Build(Server server,int port) { return Build(server,port,null); }
  internal static string Build(Server server,int port,Dictionary<string,object> proxyOverride) {
   if(port<1 || port>65535) throw new SafeException("Неверный порт проверки.");
   var config=Json.Read<Dictionary<string,object>>(Configuration.Build(server,proxyOverride??(server.Transport=="xhttp"?XrayBridge.Socks(10808,"check","synthetic-check"):null)));
   config["inbounds"]=new object[]{Configuration.Obj("type","mixed","tag","check-in","listen","127.0.0.1","listen_port",port)};
   return Json.Write(config);
  }
  internal static async Task<bool> Run(Server server,CancellationToken token) {
   string corePath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"sing-box.exe");
   if(!File.Exists(corePath)) throw new SafeException("Рядом с приложением не найдено сетевое ядро.");
   var listener=new TcpListener(IPAddress.Loopback,0); listener.Start(); int port=((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
   XrayBridge bridge=null;
   string config=Build(server,port);
   using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(token))
   using(var coreLock=new FileStream(corePath,FileMode.Open,FileAccess.Read,FileShare.Read))
   using(var process=new Process { StartInfo=Configuration.StartInfo(corePath,"run") }) {
    Configuration.VerifyCore(corePath);
    timeout.CancelAfter(20000);
    IDisposable job=null;
    try {
     if(server.Transport=="xhttp") { bridge=new XrayBridge(server,timeout.Token); config=Build(server,port,bridge.Outbound); }
     process.OutputDataReceived+=delegate { }; process.ErrorDataReceived+=delegate { };
     process.Start(); job=Native.GuardChild(process); process.BeginOutputReadLine(); process.BeginErrorReadLine();
     using(var input=new StreamWriter(process.StandardInput.BaseStream,new UTF8Encoding(false))) input.Write(config);
     bool ready=false;
     for(int i=0;i<40 && !process.HasExited;i++) {
      timeout.Token.ThrowIfCancellationRequested();
      using(var client=new TcpClient()) {
       Task connecting=client.ConnectAsync(IPAddress.Loopback,port);
       try { if(await Task.WhenAny(connecting,Task.Delay(150,timeout.Token))==connecting) { await connecting; ready=true; break; } }
       catch(SocketException) {}
       finally { ObserveFailure(connecting); }
      }
      await Task.Delay(100,timeout.Token);
     }
     if(!ready || process.HasExited) return false;
     using(var handler=new HttpClientHandler { UseProxy=true,Proxy=new WebProxy("http://127.0.0.1:"+port),UseCookies=false,AllowAutoRedirect=false })
     using(var http=new HttpClient(handler)) {
      foreach(string url in new[]{"https://cp.cloudflare.com/generate_204","https://www.gstatic.com/generate_204"}) {
       using(var requestTimeout=CancellationTokenSource.CreateLinkedTokenSource(timeout.Token)) {
        requestTimeout.CancelAfter(7000);
        try { using(var response=await http.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,requestTimeout.Token)) if((int)response.StatusCode==204 && !process.HasExited) return true; }
        catch(OperationCanceledException) { if(timeout.IsCancellationRequested) return false; }
        catch(HttpRequestException) {}
       }
      }
     }
     return false;
    } catch(OperationCanceledException) { return false; }
    finally {
     try { if(!process.HasExited) { process.Kill(); process.WaitForExit(3000); } } catch {}
     if(job!=null) job.Dispose(); if(bridge!=null) bridge.Dispose();
    }
   }
  }
  static void ObserveFailure(Task task) { task.ContinueWith(t=> { var ignored=t.Exception; },TaskContinuationOptions.OnlyOnFaulted); }
 }
}
