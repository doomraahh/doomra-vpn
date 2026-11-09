using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace Luna {
 internal static class TransportRoundtripTests {
  const string Id="11111111-1111-4111-8111-111111111111",Marker="doomra-transport-roundtrip";
  static Dictionary<string,object> O(params object[] args) {return Configuration.Obj(args);}
  static int Port() {var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();return port;}
  sealed class Core : IDisposable {
   Process process;IDisposable job;
   internal Core(string json,bool xray,int readyPort) {
    string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,xray?"xray.exe":"sing-box.exe");
    if(xray) XrayBridge.Check(json);else Configuration.Check(path,json);
    try {
     var info=xray?new ProcessStartInfo(path,"run -format json -config stdin:") {UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true}:Configuration.StartInfo(path,"run");
     process=new Process {StartInfo=info};process.OutputDataReceived+=delegate{};process.ErrorDataReceived+=delegate{};
     process.Start();job=Native.GuardChild(process);process.BeginOutputReadLine();process.BeginErrorReadLine();
     using(var input=new StreamWriter(process.StandardInput.BaseStream,new UTF8Encoding(false))) input.Write(json);
     if(readyPort==0) {Thread.Sleep(300);if(process.HasExited) throw new Exception("Synthetic server exited");return;}
     for(int i=0;i<60 && !process.HasExited;i++) {
      try {using(var tcp=new TcpClient()) {tcp.Connect(IPAddress.Loopback,readyPort);return;}} catch(SocketException) {Thread.Sleep(50);}
     }
     throw new Exception("Synthetic listener did not start");
    } catch {Dispose();throw;}
   }
   public void Dispose() {if(process!=null) {try {if(!process.HasExited) {process.Kill();process.WaitForExit(3000);}}finally{process.Dispose();process=null;}}if(job!=null){job.Dispose();job=null;}}
  }
  static byte[] Read(Stream stream,int count) {var bytes=new byte[count];int offset=0;while(offset<count){int n=stream.Read(bytes,offset,count-offset);if(n==0) throw new Exception("Unexpected synthetic EOF");offset+=n;}return bytes;}
  static void Transfer(int proxyPort,string username,string password,Action<bool> assert) {
   var origin=new TcpListener(IPAddress.Loopback,0);origin.Start();int originPort=((IPEndPoint)origin.LocalEndpoint).Port;
   var response=Task.Run(()=> {
    using(var accepted=origin.AcceptTcpClient()) using(var stream=accepted.GetStream()) {
     stream.ReadTimeout=8000;var request=new StringBuilder();
     while(!request.ToString().EndsWith("\r\n\r\n",StringComparison.Ordinal) && request.Length<8192) request.Append((char)Read(stream,1)[0]);
     byte[] payload=Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: "+Marker.Length+"\r\nConnection: close\r\n\r\n"+Marker);
     stream.Write(payload,0,payload.Length);
    }
   });
   try {
    using(var tcp=new TcpClient()) {
     tcp.ReceiveTimeout=8000;tcp.SendTimeout=8000;tcp.Connect(IPAddress.Loopback,proxyPort);
     using(var stream=tcp.GetStream()) {
      stream.Write(new byte[]{5,1,2},0,3);var greeting=Read(stream,2);assert(greeting[0]==5 && greeting[1]==2);
      byte[] user=Encoding.ASCII.GetBytes(username),pass=Encoding.ASCII.GetBytes(password);
      var auth=new List<byte>{1,(byte)user.Length};auth.AddRange(user);auth.Add((byte)pass.Length);auth.AddRange(pass);stream.Write(auth.ToArray(),0,auth.Count);assert(Read(stream,2)[1]==0);
      byte[] connect={5,1,0,1,127,0,0,1,(byte)(originPort>>8),(byte)originPort};stream.Write(connect,0,connect.Length);
      var reply=Read(stream,4);assert(reply[0]==5 && reply[1]==0);
      int addressSize=reply[3]==1?4:(reply[3]==4?16:Read(stream,1)[0]);Read(stream,addressSize+2);
      byte[] get=Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n");stream.Write(get,0,get.Length);
      var header=new StringBuilder();
      while(!header.ToString().EndsWith("\r\n\r\n",StringComparison.Ordinal) && header.Length<8192) header.Append((char)Read(stream,1)[0]);
      assert(header.ToString().StartsWith("HTTP/1.1 200 OK",StringComparison.Ordinal));
      assert(Encoding.ASCII.GetString(Read(stream,Marker.Length))==Marker);
     }
    }
    assert(response.Wait(8000));response.GetAwaiter().GetResult();
   } finally {origin.Stop();response.ContinueWith(t=> {var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);}
  }
  static string Pem(string label,byte[] data) {return "-----BEGIN "+label+"-----\n"+Convert.ToBase64String(data,Base64FormattingOptions.InsertLineBreaks)+"\n-----END "+label+"-----";}
  internal static void Run(Action<string,Action> test,Action<bool> assert) {
   foreach(string transport in new[]{"grpc","grpc-multi","httpupgrade"}) {
    string choice=transport;
    test("Real loopback bytes through Xray "+choice,delegate {
     bool grpc=choice.StartsWith("grpc",StringComparison.Ordinal);int port=Port();
     var stream=O("network",grpc?"grpc":"httpupgrade","security","none");
     stream[grpc?"grpcSettings":"httpupgradeSettings"]=grpc?O("serviceName","unit","multiMode",choice=="grpc-multi"):O("host","unit.invalid","path","/unit");
     // Pinned Xray blocks loopback destinations by default. Allow only the local
     // test origin in this synthetic server; production/VPS rules are unchanged.
     string server=Json.Write(O("log",O("loglevel","none"),"inbounds",new object[]{O("listen","127.0.0.1","port",port,"protocol","vless","settings",O("decryption","none","clients",new object[]{O("id",Id)}),"streamSettings",stream)},"outbounds",new object[]{O("protocol","freedom","settings",O("finalRules",new object[]{O("action","allow","network","tcp","ip",new[]{"127.0.0.1/32"})}))}));
     var s=new Server {Protocol="vless",Host="127.0.0.1",Port=port,Uuid=Id,Security="none",Transport=grpc?"grpc":"httpupgrade",ServiceName=grpc?"unit":"",GrpcMultiMode=choice=="grpc-multi",Path=grpc?"":"/unit",TransportHost=grpc?"":"unit.invalid"};
     using(var remote=new Core(server,true,port)) using(var bridge=new XrayBridge(s,CancellationToken.None)) Transfer(bridge.Port,bridge.Username,bridge.Password,assert);
    });
   }
   foreach(string protocol in new[]{"tuic","anytls","hysteria2"}) {
    string choice=protocol;
    test("Real loopback bytes through native "+choice+" with verified TLS",delegate {
     using(var rsa=new RSACng(2048)) {
      var request=new CertificateRequest("CN=localhost",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
      request.CertificateExtensions.Add(new X509Extension("2.5.29.17",new byte[]{0x30,0x0b,0x82,0x09,108,111,99,97,108,104,111,115,116},false));
      using(var certificate=request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5),DateTimeOffset.UtcNow.AddHours(1))) {
       string cert=Pem("CERTIFICATE",certificate.Export(X509ContentType.Cert));byte[] key=rsa.Key.Export(CngKeyBlobFormat.Pkcs8PrivateBlob);string keyPem;
       try {keyPem=Pem("PRIVATE KEY",key);}finally{Array.Clear(key,0,key.Length);}
       int port=Port(),local=Port();
       var user=O("name","unit","password","synthetic");if(choice=="tuic") user["uuid"]=Id;
       var inbound=O("type",choice,"listen","127.0.0.1","listen_port",port,"users",new object[]{user},"tls",O("enabled",true,"certificate",new[]{cert},"key",new[]{keyPem}));
       if(choice!="anytls") ((Dictionary<string,object>)inbound["tls"])["alpn"]=new[]{"h3"};
       string remoteConfig=Json.Write(O("log",O("disabled",true),"inbounds",new object[]{inbound},"outbounds",new object[]{O("type","direct")}));
       var s=new Server {Protocol=choice,Host="127.0.0.1",Port=port,Uuid=choice=="tuic"?Id:"",Password="synthetic",Security="tls",Sni="localhost",Alpn=choice=="anytls"?new string[0]:new[]{"h3"}};
       var outbound=Configuration.BuildOutbound(s);((Dictionary<string,object>)outbound["tls"])["certificate"]=new[]{cert};
       string client=ServerCheck.Build(s,local,outbound,new NetworkCredential("unit","synthetic"));
       using(var remote=new Core(remoteConfig,false,choice=="anytls"?port:0)) using(var proxy=new Core(client,false,local)) Transfer(local,"unit","synthetic",assert);
      }
     }
    });
   }
  }
 }
}
