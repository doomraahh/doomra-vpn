using System;
using System.Linq;
using System.Collections.Generic;
namespace Luna {
 internal static class ExtendedProtocols {
  internal static void Validate(Server s) {
   if(s.Transport=="xhttp" && s.Protocol!="vless") throw new SafeException("XHTTP поддерживается только для VLESS.");
   if(s.XhttpMode==null || !new[]{"auto","packet-up","stream-up","stream-one"}.Contains(s.XhttpMode) || (s.Transport!="xhttp" && s.XhttpMode!="auto")) throw new SafeException("Неподдерживаемый режим XHTTP.");
   if(s.Protocol=="hysteria2") {
    if(s.Security!="tls" || s.Transport!="tcp" || s.Fingerprint!="" || s.Flow!="") throw new SafeException("Hysteria2 требует TLS и собственный транспорт QUIC.");
    if(s.Obfs!="" && s.Obfs!="salamander") throw new SafeException("Поддерживается только обфускация salamander.");
    if(s.ObfsPassword==null || s.ObfsPassword.Length>2048 || (s.Obfs=="" ? s.ObfsPassword!="" : s.ObfsPassword.Length==0)) throw new SafeException("Неверный пароль обфускации Hysteria2.");
   } else if(s.Obfs!="" || s.ObfsPassword!="") throw new SafeException("Обфускация Hysteria2 без Hysteria2.");
  }
 }
 internal static class XrayConfig {
  static Dictionary<string,object> O(params object[] pairs) { return Configuration.Obj(pairs); }
  internal static string Build(Server s,int port,string username,string password) {
   Subscriptions.Validate(s); Subscriptions.CheckProviderNotice(s.Name);
   if(s.Transport!="xhttp" || s.Protocol!="vless") throw new SafeException("Мост Xray предназначен для VLESS XHTTP.");
   if(port<1 || port>65535 || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || username.Length>255 || password.Length>255) throw new SafeException("Неверные параметры локального моста.");
   var stream=O("network","xhttp","security",s.Security==""?"none":s.Security,"xhttpSettings",O("host",s.TransportHost,"path",s.Path==""?"/":s.Path,"mode",s.XhttpMode),"sockopt",O("domainStrategy","UseIPv4"));
   if(s.Security=="tls") { var tls=O("serverName",s.Sni==""?s.Host:s.Sni,"allowInsecure",false); if(s.Fingerprint!="") tls["fingerprint"]=s.Fingerprint; if(s.Alpn.Length>0) tls["alpn"]=s.Alpn; stream["tlsSettings"]=tls; }
   if(s.Security=="reality") stream["realitySettings"]=O("serverName",s.Sni,"fingerprint",s.Fingerprint==""?"chrome":s.Fingerprint,"publicKey",s.PublicKey,"shortId",s.ShortId);
   return Json.Write(O("log",O("loglevel","none"),"dns",O("servers",new[]{"https+local://1.1.1.1/dns-query"},"queryStrategy","UseIPv4"),
    "inbounds",new object[]{O("listen","127.0.0.1","port",port,"protocol","socks","settings",O("auth","password","accounts",new object[]{O("user",username,"pass",password)},"udp",true,"ip","127.0.0.1"))},
    "outbounds",new object[]{O("protocol","vless","settings",O("vnext",new object[]{O("address",s.Host,"port",s.Port,"users",new object[]{O("id",s.Uuid,"encryption","none")})}),"streamSettings",stream)}));
  }
 }
}
