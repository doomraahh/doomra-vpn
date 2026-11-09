using System;
using System.IO;
using System.Text;
namespace Luna {
 internal static class CompatibilityTests {
  const string Id="11111111-1111-4111-8111-111111111111";
  static Server One(string text) {var r=Subscriptions.Parse(text);if(r.Servers.Count!=1 || r.Errors.Count!=0) throw new Exception("Expected one supported synthetic server");return r.Servers[0];}
  static void Check(Server server) {
   bool bridge=XrayBridge.Required(server);
   if(bridge) XrayBridge.Check(XrayConfig.Build(server,10808,"user","synthetic"));
   Configuration.Check(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"sing-box.exe"),Configuration.Build(server,bridge?XrayBridge.Socks(10808,"user","synthetic"):null));
  }
  internal static void Run(Action<string,Action> test,Action<bool> assert) {
   test("Transport-specific YAML options cannot silently become a different transport",delegate {
    string body="proxies:\n  - name: Test\n    type: vless\n    server: example.com\n    port: 443\n    uuid: "+Id+"\n    network: xhttp\n    ws-opts:\n      path: /wrong\n";
    assert(Subscriptions.Parse(body).Servers.Count==0);
    var invalid=new Server {Protocol="anytls",Host="example.com",Port=443,Password="synthetic",Security="none"};
    bool rejected=false;try {Subscriptions.Validate(invalid);}catch(SafeException){rejected=true;}assert(rejected);
   });
   test("gRPC URI service path and authority host aliases preserve transport",delegate {
    var s=One("vless://"+Id+"@example.com:443?type=grpc&security=tls&path=service&host=grpc.example.com&mode=multi");
    assert(s.ServiceName=="service" && s.GrpcAuthority=="grpc.example.com" && s.GrpcMultiMode && s.Path=="" && s.TransportHost=="");Check(s);
    assert(Subscriptions.Parse("vless://"+Id+"@example.com:443?type=grpc&serviceName=one&path=two").Servers.Count==0);
   });
   test("VMess gRPC JSON links map path host and multi mode",delegate {
    string body=Json.Write(Configuration.Obj("v","2","add","example.com","port","443","id",Id,"net","grpc","tls","tls","type","multi","path","service","host","grpc.example.com"));
    var s=One("vmess://"+Convert.ToBase64String(Encoding.UTF8.GetBytes(body)));
    assert(s.ServiceName=="service" && s.GrpcAuthority=="grpc.example.com" && s.GrpcMultiMode);Check(s);
   });
   test("Xray JSON accepts named gRPC mode and HTTPUpgrade settings",delegate {
    string endpoint="\"protocol\":\"vless\",\"settings\":{\"address\":\"example.com\",\"port\":443,\"id\":\""+Id+"\"}";
    var grpc=One("{\"outbounds\":[{"+endpoint+",\"streamSettings\":{\"network\":\"grpc\",\"security\":\"tls\",\"grpcSettings\":{\"serviceName\":\"vpn\",\"mode\":\"multi\"}}}]}");
    assert(grpc.GrpcMultiMode);Check(grpc);
    var upgrade=One("{\"outbounds\":[{"+endpoint+",\"streamSettings\":{\"network\":\"httpupgrade\",\"security\":\"tls\",\"httpupgradeSettings\":{\"path\":\"/vpn\",\"host\":\"cdn.example.com\"}}}]}");
    assert(upgrade.Transport=="httpupgrade" && upgrade.Path=="/vpn");Check(upgrade);
   });
   test("HTTPUpgrade URI generates the exact Xray transport",delegate {
    var s=One("vless://"+Id+"@example.com:443?type=httpupgrade&security=tls&path=%2Fvpn&host=cdn.example.com");
    string json=XrayConfig.Build(s,10808,"user","synthetic");assert(json.Contains("httpupgradeSettings") && json.Contains("/vpn"));Check(s);
   });
   test("TUIC v5 and AnyTLS URIs generate native sing-box outbounds",delegate {
    var tuic=One("tuic://"+Id+":synthetic@example.com:443?sni=example.com&alpn=h3&congestion_control=bbr&udp_relay_mode=native");
    assert(tuic.Protocol=="tuic" && tuic.Uuid==Id && tuic.Password=="synthetic" && !XrayBridge.Required(tuic));
    var outbound=Configuration.BuildOutbound(tuic);assert((string)outbound["congestion_control"]=="bbr");Check(tuic);
    var any=One("anytls://synthetic@example.com?sni=example.com&fp=chrome");assert(any.Port==443 && any.Security=="tls" && !XrayBridge.Required(any));Check(any);
    var hy=One("hy2://synthetic@example.com?alpn=h3&insecure=false");assert(hy.Alpn.Length==1 && hy.Alpn[0]=="h3");Check(hy);
    foreach(string bad in new[]{"tuic://bad:synthetic@example.com","tuic://"+Id+":synthetic@example.com?congestion_control=unknown","anytls://synthetic@example.com?insecure=1","anytls://synthetic@example.com?type=grpc"}) assert(Subscriptions.Parse(bad).Servers.Count==0);
   });
   test("Clash imports TUIC AnyTLS and Hysteria2 with ALPN without executing other sections",delegate {
    string prefix="proxies:\n  - name: Test\n    server: example.com\n    port: 443\n    password: synthetic\n    sni: example.com\n";
    var tuic=One(prefix+"    type: tuic\n    uuid: "+Id+"\n    congestion-controller: bbr\n    udp-relay-mode: native\n    alpn: [h3]\n");Check(tuic);
    var any=One(prefix+"    type: anytls\n    client-fingerprint: chrome\n");Check(any);
    var hy=One(prefix+"    type: hysteria2\n    alpn:\n      - h3\n    obfs: salamander\n    obfs-password: synthetic\n");assert(hy.Alpn.Length==1 && hy.Alpn[0]=="h3");Check(hy);
    var encoded=One(Convert.ToBase64String(Encoding.UTF8.GetBytes(prefix+"    type: anytls\n")));assert(encoded.Protocol=="anytls");
   });
  }
 }
}
