using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace Luna {
 // Restricted block-YAML reader for Clash proxy entries only. No YAML tags,
 // anchors, constructors, rules, scripts, providers or local paths are executed.
 internal static class ClashImport {
  static void Need(bool ok) { if(!ok) throw new SafeException("Неподдерживаемая структура YAML-сервера (содержимое скрыто)."); }
  static string Scalar(string input) {
   string s=input.Trim(); Need(s.Length<=2048 && !s.Any(char.IsControl));
   if(s.StartsWith("\"")) { try { return Json.Read<string>(s); } catch { throw new SafeException("Неверная строка YAML (содержимое скрыто)."); } }
   if(s.StartsWith("'")) { Need(s.Length>=2 && s.EndsWith("'")); return s.Substring(1,s.Length-2).Replace("''", "'"); }
   Need(!s.StartsWith("!") && !s.StartsWith("&") && !s.StartsWith("*") && !s.StartsWith("{") && !s.StartsWith("[") && s!="|" && s!=">");
   int comment=s.IndexOf(" #",StringComparison.Ordinal); return comment<0?s:s.Substring(0,comment).TrimEnd();
  }
  static string Get(Dictionary<string,string> d,string k,string fallback="") { string v; return d.TryGetValue(k,out v)?v:fallback; }
  static bool Bool(Dictionary<string,string> d,string k,bool fallback=false) { string s=Get(d,k,fallback?"true":"false"); Need(s=="true" || s=="false"); return s=="true"; }
  static void Keys(Dictionary<string,string> d,string allowed) { var set=new HashSet<string>(allowed.Split(',')); Need(d.Keys.All(set.Contains)); }
  static Dictionary<string,string> Fields(List<string> lines,out string[] alpn) {
   var d=new Dictionary<string,string>();var stack=new List<KeyValuePair<int,string>>();var sections=new HashSet<string>();var protocols=new List<string>();int baseIndent=-1;
   foreach(string line in lines) {
    int indent=line.TakeWhile(c=>c==' ').Count(); string value=line.Trim();
    if(value.StartsWith("- ") && baseIndent>=0 && indent>baseIndent) {Need(stack.Count==1 && stack[0].Value=="alpn");protocols.Add(Scalar(value.Substring(2)));Need(protocols.Count<=8);continue;}
    if(value.StartsWith("- ")) { value=value.Substring(2).TrimStart(); indent+=2; }
    if(baseIndent<0) baseIndent=indent;
    Need(indent>=baseIndent && !line.Contains('\t'));
    var match=Regex.Match(value,@"^([A-Za-z][A-Za-z0-9_-]*):(?:\s+(.*)|\s*)$"); Need(match.Success);
    string key=match.Groups[1].Value,raw=match.Groups[2].Value;
    while(stack.Count>0 && indent<=stack[stack.Count-1].Key) stack.RemoveAt(stack.Count-1);
    if(stack.Count>0) key=string.Join(".",stack.Select(s=>s.Value))+"."+key;
    if(raw=="") {
     Need(new[]{"reality-opts","grpc-opts","ws-opts","ws-opts.headers","http-upgrade-opts","alpn"}.Contains(key) && sections.Add(key) && !d.ContainsKey(key));
     stack.Add(new KeyValuePair<int,string>(indent,match.Groups[1].Value));continue;
    }
    if(key=="alpn") {
     Need(sections.Add(key) && raw.StartsWith("[") && raw.EndsWith("]"));
     string list=raw.Substring(1,raw.Length-2).Trim();if(list!="") protocols.AddRange(list.Split(',').Select(Scalar));Need(protocols.Count<=8);continue;
    }
    Need(!d.ContainsKey(key)); d.Add(key,Scalar(raw));
   }
   alpn=protocols.ToArray();return d;
  }
  static Server Read(List<string> lines) {
   string[] alpn;var d=Fields(lines,out alpn);
   Keys(d,"name,type,server,port,uuid,password,cipher,alterId,network,tls,servername,sni,client-fingerprint,flow,udp,skip-cert-verify,encryption,obfs,obfs-password,congestion-controller,udp-relay-mode,reality-opts.public-key,reality-opts.short-id,reality-opts.support-x25519mlkem768,grpc-opts.grpc-service-name,grpc-opts.authority,grpc-opts.mode,ws-opts.path,ws-opts.headers.Host,http-upgrade-opts.path,http-upgrade-opts.host");
   int port; Need(int.TryParse(Get(d,"port"),out port));
   string protocol=Get(d,"type");if(protocol=="hy2") protocol="hysteria2";Need(new[]{"vless","vmess","trojan","ss","tuic","anytls","hysteria2"}.Contains(protocol));
   Need(!Bool(d,"skip-cert-verify") && Get(d,"encryption","none")=="none");
   Bool(d,"udp"); Bool(d,"reality-opts.support-x25519mlkem768");
   var server=new Server { Name=Subscriptions.CleanName(Get(d,"name")),Protocol=protocol,Host=Get(d,"server"),Port=port,Uuid=Get(d,"uuid"),Password=Get(d,"password"),Method=Get(d,"cipher"),Transport=Get(d,"network","tcp"),Sni=Get(d,"servername",Get(d,"sni")),Fingerprint=Get(d,"client-fingerprint"),Flow=Get(d,"flow"),PublicKey=Get(d,"reality-opts.public-key"),ShortId=Get(d,"reality-opts.short-id"),ServiceName=Get(d,"grpc-opts.grpc-service-name"),GrpcAuthority=Get(d,"grpc-opts.authority"),Path=Get(d,"ws-opts.path") };
   server.Alpn=alpn;server.TransportHost=Get(d,"ws-opts.headers.Host");server.Obfs=Get(d,"obfs");server.ObfsPassword=Get(d,"obfs-password");
   server.CongestionControl=Get(d,"congestion-controller","cubic");server.UdpRelayMode=Get(d,"udp-relay-mode","native");
   Need(server.Transport=="ws" || !d.Keys.Any(k=>k.StartsWith("ws-opts.",StringComparison.Ordinal)));
   if(server.Transport=="httpupgrade") {Need(server.Path=="" && server.TransportHost=="");server.Path=Get(d,"http-upgrade-opts.path");server.TransportHost=Get(d,"http-upgrade-opts.host");}
   else Need(!d.ContainsKey("http-upgrade-opts.path") && !d.ContainsKey("http-upgrade-opts.host"));
   if(server.Transport=="raw") server.Transport="tcp";
   if(protocol=="vmess") {Need(Get(d,"alterId","0")=="0");server.Cipher=Get(d,"cipher","auto");server.Method="";}
   else Need(!d.ContainsKey("alterId") && (protocol=="ss" || !d.ContainsKey("cipher")));
   server.Security=server.PublicKey!=""?"reality":Bool(d,"tls",new[]{"trojan","hysteria2","tuic","anytls"}.Contains(protocol))?"tls":"none";
   if(server.Security=="reality") Need(Bool(d,"tls"));
   string mode=Get(d,"grpc-opts.mode","gun"); Need(mode=="gun" || mode=="multi");server.GrpcMultiMode=mode=="multi";
   Need(server.Transport=="grpc" || !d.Keys.Any(k=>k.StartsWith("grpc-opts.",StringComparison.Ordinal)));
   Subscriptions.Validate(server); Subscriptions.CheckProviderNotice(server.Name); if(server.Name=="") server.Name=server.Protocol.ToUpperInvariant();
   return server;
  }
  internal static ImportResult Parse(string text) {
   var result=new ImportResult(); var entries=new List<List<string>>(); List<string> entry=null; bool proxies=false; int listIndent=-1;
   foreach(string raw in text.Replace("\r","").Split('\n')) {
    string value=raw.Trim(); if(value=="" || value.StartsWith("#")) continue;
    if(Regex.IsMatch(raw,@"^proxies\s*:\s*(?:#.*)?$")) { Need(!proxies);proxies=true;continue; }
    if(!proxies) continue;
    int indent=raw.TakeWhile(c=>c==' ').Count();
    if(indent==0 && !value.StartsWith("- ")) break;
    if(value.StartsWith("- ") && (listIndent<0 || indent==listIndent)) { listIndent=indent;entry=new List<string>();entries.Add(entry);Need(entries.Count<=10000); }
    Need(entry!=null);entry.Add(raw);
   }
   Need(proxies && entries.Count>0); var unique=new HashSet<string>();
   foreach(var item in entries) {
    try { var server=Read(item); Subscriptions.AddImported(result,server,unique); }
    catch(SafeException e) { result.Errors.Add("YAML-сервер "+(result.Servers.Count+result.Errors.Count+1)+": "+e.Message); }
    catch { result.Errors.Add("Неверный YAML-сервер (содержимое скрыто)."); }
   }
   result.Notes.Add("Из YAML импортированы только серверы; группы, правила, DNS и скрипты подписки не применяются.");
   return result;
  }
 }
}
