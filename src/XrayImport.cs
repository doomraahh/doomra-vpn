using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace Luna {
 // Extract a typed, validated server model. Never execute or pass through the
 // subscription's inbounds, routing, DNS, selectors, local paths or commands.
 internal static class XrayImport {
  static void Need(bool ok,string message) { if(!ok) throw new SafeException(message); }
  static Dictionary<string,object> Map(object value) { var d=value as Dictionary<string,object>; Need(d!=null,"Ожидается JSON-объект."); return d; }
  static object Value(Dictionary<string,object> d,string key) { object v; return d.TryGetValue(key,out v)?v:null; }
  static Dictionary<string,object> Child(Dictionary<string,object> d,string key) { object v=Value(d,key); return v==null?new Dictionary<string,object>():Map(v); }
  static string Text(Dictionary<string,object> d,string key,string fallback="") { object v=Value(d,key); if(v==null) return fallback; Need(v is string || v is int || v is long,"Неверный тип JSON-поля."); return Convert.ToString(v,System.Globalization.CultureInfo.InvariantCulture); }
  static int Number(Dictionary<string,object> d,string key,int fallback=0) { string s=Text(d,key,fallback.ToString()); int n; Need(int.TryParse(s,out n),"Неверное числовое JSON-поле."); return n; }
  static bool Enabled(Dictionary<string,object> d,string key) { object v=Value(d,key); if(v==null) return false; Need(v is bool,"Неверный тип логического JSON-поля."); return (bool)v; }
  static object[] Items(object v) { Need(v is IEnumerable && !(v is string) && !(v is IDictionary),"Ожидается JSON-массив."); return ((IEnumerable)v).Cast<object>().ToArray(); }
  static Dictionary<string,object> Single(object v) { object[] items=Items(v); Need(items.Length==1,"Несколько адресов или пользователей в одном outbound не поддерживаются."); return Map(items[0]); }
  static void Keys(Dictionary<string,object> d,string allowed,[System.Runtime.CompilerServices.CallerLineNumber] int location=0,string section=null) {
   var set=new HashSet<string>(allowed.Split(','));
   Need(d.Keys.All(set.Contains),"Есть неподдерживаемые параметры JSON-сервера ("+(section??"код "+location)+", содержимое скрыто).");
  }
  static void Note(ImportResult r,string note) { if(!r.Notes.Contains(note)) r.Notes.Add(note); }
  public static ImportResult Parse(string text) {
   var result=new ImportResult(); object parsed;
   try { parsed=Json.Codec().DeserializeObject(text); } catch { throw new SafeException("Неверный или слишком глубоко вложенный JSON подписки (содержимое скрыто)."); }
   object[] configs=parsed is IDictionary?new[]{parsed}:Items(parsed);
   Need(configs.Length>0 && configs.Length<=1000,"Превышено ограничение числа JSON-конфигураций.");
   Note(result,"Из JSON извлечены только серверы. Входящие соединения, DNS, маршруты, селекторы и правила подписки не применяются; VPN создаёт собственную конфигурацию.");
   var unique=new HashSet<string>(); int index=0,configIndex=0;
   foreach(object configValue in configs) {
    configIndex++;
    try {
    var config=Map(configValue); object outboundValue=Value(config,"outbounds");
    Need(outboundValue!=null,"JSON не содержит Xray outbounds. Другие JSON-форматы пока не поддерживаются.");
    object[] outboundItems=Items(outboundValue);
    var tagged=new Dictionary<string,Dictionary<string,object>>(StringComparer.Ordinal);
    foreach(object item in outboundItems) { var candidate=Map(item); string tag=Text(candidate,"tag"); if(tag!="") { Need(!tagged.ContainsKey(tag),"Повторяющийся тег outbound (значение скрыто)."); tagged.Add(tag,candidate); } }
    var referenced=new HashSet<string>(StringComparer.Ordinal);
    foreach(object item in outboundItems) {var candidate=Map(item);string tag=Text(Child(Child(candidate,"streamSettings"),"sockopt"),"dialerProxy",Text(Child(candidate,"proxySettings"),"tag"));if(tag!="") referenced.Add(tag);}
    foreach(object item in outboundItems) {
     index++; if(index>10000) { result.Errors.Add("Превышено ограничение числа JSON-серверов."); return result; }
     try {
      var outbound=Map(item); string protocol=Text(outbound,"protocol");
      if(new[]{"freedom","blackhole","dns","loopback"}.Contains(protocol)) continue;
      Server s=ResolveServer(outbound,Text(config,"remarks"),result,index,tagged,new HashSet<Dictionary<string,object>>(),0);
      Subscriptions.Validate(s); Subscriptions.CheckProviderNotice(s.Name); s.Name=ServerNames.FromJson(Text(config,"remarks"),s.Name,s); Subscriptions.CheckProviderNotice(s.Name);
      if(referenced.Contains(Text(outbound,"tag"))) continue;
      Subscriptions.AddImported(result,s,unique);
     } catch(SafeException e) { result.Errors.Add("JSON-сервер "+index+": "+e.Message); }
     catch { result.Errors.Add("JSON-сервер "+index+": неверное значение (содержимое скрыто)."); }
    }
    } catch(SafeException e) { result.Errors.Add("JSON-конфигурация "+configIndex+": "+e.Message); }
    catch { result.Errors.Add("JSON-конфигурация "+configIndex+": неверная структура (содержимое скрыто)."); }
   }
   return result;
  }
  static Server ResolveServer(Dictionary<string,object> outbound,string remarks,ImportResult result,int index,Dictionary<string,Dictionary<string,object>> tagged,HashSet<Dictionary<string,object>> visited,int depth) {
   Need(depth<4 && visited.Add(outbound),"Каскад содержит цикл или больше четырёх узлов.");
   var stream=Child(outbound,"streamSettings"); var socket=Child(stream,"sockopt");
   Keys(socket,"dialerProxy,domainStrategy");
   Need(new[]{"","AsIs","UseIPv4"}.Contains(Text(socket,"domainStrategy")),"Стратегия сокета не поддерживается.");
   var proxy=Child(outbound,"proxySettings"); Keys(proxy,"tag,transportLayer");
   string dialer=Text(socket,"dialerProxy"),legacy=Text(proxy,"tag");
   Need(dialer=="" || legacy=="","Повторяющееся указание каскада.");
   Need(proxy.Count==0 || (legacy!="" && Enabled(proxy,"transportLayer")),"Каскад proxySettings требует transportLayer=true.");
   var clean=new Dictionary<string,object>(outbound); clean.Remove("proxySettings");
   var cleanStream=new Dictionary<string,object>(stream); cleanStream.Remove("sockopt"); clean["streamSettings"]=cleanStream;
   Server server=ReadServer(clean,remarks,result,index);
   string reference=dialer==""?legacy:dialer;
   if(reference!="") {
    Dictionary<string,object> entry;
    Need(tagged.TryGetValue(reference,out entry),"В подписке отсутствует узел каскада (тег скрыт).");
    server.Dialer=ResolveServer(entry,"",result,index,tagged,visited,depth+1);
   }
   return server;
  }
  static Server ReadServer(Dictionary<string,object> outbound,string remarks,ImportResult result,int index) {
   string protocol=Text(outbound,"protocol"); bool hysteria=protocol=="hysteria";
   Need(new[]{"vless","vmess","trojan","shadowsocks","hysteria"}.Contains(protocol),"Протокол JSON-outbound не поддерживается.");
   var stream=Child(outbound,"streamSettings"); string network=Text(stream,"network",Text(stream,"method","tcp"));
   Need(Value(stream,"method")==null || Text(stream,"method")==network,"Конфликт network и method транспорта.");
   if(network=="raw") network="tcp";
   if(network=="splithttp") network="xhttp";
   Need(hysteria?network=="hysteria":new[]{"tcp","ws","grpc","xhttp","httpupgrade"}.Contains(network),"Транспорт JSON-сервера не поддерживается.");
   Keys(outbound,"protocol,tag,settings,streamSettings,mux,sendThrough,proxySettings");
   Need(new[]{"","0.0.0.0","::"}.Contains(Text(outbound,"sendThrough")),"Привязка outbound к исходному адресу не поддерживается.");
   Need(Child(outbound,"proxySettings").Count==0,"Цепочка proxySettings не поддерживается.");
   var mux=Child(outbound,"mux"); Keys(mux,"enabled,concurrency,xudpConcurrency,xudpProxyUDP443"); Need(!Enabled(mux,"enabled"),"Включённый Xray mux не поддерживается.");
   Keys(stream,"network,method,security,tlsSettings,realitySettings,tcpSettings,rawSettings,wsSettings,grpcSettings,httpupgradeSettings,xhttpSettings,splithttpSettings,sockopt,hysteriaSettings");
   Need(Child(stream,"sockopt").Count==0,"Параметры sockopt не переносятся; сервер отклонён.");
   Need(hysteria || Child(stream,"hysteriaSettings").Count==0,"Параметры Hysteria без Hysteria.");
   var s=new Server { Protocol=hysteria?"hysteria2":(protocol=="shadowsocks"?"ss":protocol),Transport=hysteria?"tcp":network,Security=Text(stream,"security","none") };
   s.Name=Text(outbound,"tag",remarks);
   var settings=Child(outbound,"settings");
   if(hysteria) {
    Keys(settings,"version,address,port");
    var transport=Child(stream,"hysteriaSettings"); Keys(transport,"version,auth,udpIdleTimeout");
    Need(Number(settings,"version")==2 && Number(transport,"version")==2,"Поддерживается только Hysteria версии 2.");
    s.Host=Text(settings,"address"); s.Port=Number(settings,"port"); s.Password=Text(transport,"auth");
   } else if(protocol=="vless" || protocol=="vmess") {
    Dictionary<string,object> endpoint,user;
    if(Value(settings,"vnext")!=null) {
     Keys(settings,"vnext"); endpoint=Single(Value(settings,"vnext")); Keys(endpoint,"address,port,users"); user=Single(Value(endpoint,"users")); Keys(user,protocol=="vless"?"id,encryption,flow,level,email":"id,alterId,security,level,email");
    } else {
     Keys(settings,protocol=="vless"?"address,port,id,encryption,flow,level,email,reverse":"address,port,id,alterId,security,level,email");
     Need(Child(settings,"reverse").Count==0,"Обратное подключение VLESS не поддерживается."); endpoint=settings; user=settings;
    }
    s.Host=Text(endpoint,"address"); s.Port=Number(endpoint,"port");
    s.Uuid=Text(user,"id");
    if(protocol=="vless") { Need(Text(user,"encryption","none")=="none","VLESS encryption не поддерживается."); s.Flow=Text(user,"flow"); }
    else { Need(Number(user,"alterId")==0,"VMess alterId > 0 не поддерживается."); s.Cipher=Text(user,"security","auto"); Need(new[]{"auto","aes-128-gcm","chacha20-poly1305","none","zero"}.Contains(s.Cipher),"Шифр VMess не поддерживается."); }
   } else {
    Dictionary<string,object> endpoint;
    if(Value(settings,"servers")!=null) { Keys(settings,"servers"); endpoint=Single(Value(settings,"servers")); }
    else endpoint=settings;
    Keys(endpoint,protocol=="trojan"?"address,port,password,level,email":"address,port,password,method,level,email");
    s.Host=Text(endpoint,"address"); s.Port=Number(endpoint,"port"); s.Password=Text(endpoint,"password");
    if(protocol=="shadowsocks") { s.Method=Text(endpoint,"method"); Need(new[]{"aes-128-gcm","aes-256-gcm","chacha20-ietf-poly1305","2022-blake3-aes-128-gcm","2022-blake3-aes-256-gcm","2022-blake3-chacha20-poly1305"}.Contains(s.Method),"Шифр Shadowsocks не поддерживается."); Need(s.Security=="none" && network=="tcp","Shadowsocks с внешним TLS/транспортом не поддерживается."); }
   }
   if(s.Security=="tls") {
    Need(Child(stream,"realitySettings").Count==0,"Reality-параметры при режиме TLS не поддерживаются.");
    var tls=Child(stream,"tlsSettings"); Keys(tls,"serverName,allowInsecure,alpn,fingerprint,certificates,disableSystemRoot,enableSessionResumption");
    Need(!Enabled(tls,"allowInsecure") && !Enabled(tls,"disableSystemRoot") && !Enabled(tls,"enableSessionResumption"),"Небезопасные или неподдерживаемые настройки TLS.");
    object certs=Value(tls,"certificates"); Need(certs==null || Items(certs).Length==0,"Собственные сертификаты JSON не поддерживаются.");
    s.Sni=Text(tls,"serverName"); s.Fingerprint=Text(tls,"fingerprint"); object alpn=Value(tls,"alpn"); if(alpn!=null) s.Alpn=Items(alpn).Select(x=> { Need(x is string,"Неверный ALPN."); return (string)x; }).ToArray();
    if(hysteria && s.Fingerprint!="") { s.Fingerprint=""; Note(result,"TLS fingerprint Xray не переносится в Hysteria2/QUIC. Проверка сертификата остаётся включена."); }
   } else if(s.Security=="reality") {
    Need(Child(stream,"tlsSettings").Count==0,"TLS-параметры при режиме Reality не поддерживаются.");
    var reality=Child(stream,"realitySettings"); Keys(reality,"serverName,fingerprint,publicKey,password,shortId,spiderX,show");
    Need(!Enabled(reality,"show"),"Отладочный режим Reality не поддерживается.");
    string publicKey=Text(reality,"publicKey"),password=Text(reality,"password");
    Need(publicKey=="" || password=="" || publicKey==password,"В JSON заданы разные ключи Reality.");
    s.Sni=Text(reality,"serverName"); s.Fingerprint=Text(reality,"fingerprint"); s.PublicKey=password==""?publicKey:password; s.ShortId=Text(reality,"shortId");
    if(Text(reality,"spiderX")!="") Note(result,"Вспомогательный параметр Xray Reality spiderX не переносится в sing-box.");
   } else Need(Child(stream,"tlsSettings").Count==0 && Child(stream,"realitySettings").Count==0,"TLS-параметры без TLS/Reality.");
   if(network=="ws") {
    var ws=Child(stream,"wsSettings"); Keys(ws,"path,headers,maxEarlyData,earlyDataHeaderName");
    Need(Number(ws,"maxEarlyData")==0 && Text(ws,"earlyDataHeaderName")=="","WebSocket early data не поддерживается.");
    s.Path=Text(ws,"path"); var headers=Child(ws,"headers"); Need(headers.Count<=1 && headers.Keys.All(k=>k.Equals("Host",StringComparison.OrdinalIgnoreCase)),"Пользовательские или повторяющиеся заголовки WebSocket не поддерживаются."); if(headers.Count>0) { object host=headers.Values.First(); Need(host is string,"Неверный WebSocket Host."); s.TransportHost=(string)host; }
   } else Need(Child(stream,"wsSettings").Count==0,"WebSocket-параметры без WebSocket.");
   if(network=="grpc") {
    var grpc=Child(stream,"grpcSettings"); Keys(grpc,"serviceName,multiMode,mode,authority,user_agent,idle_timeout,health_check_timeout,permit_without_stream,initial_windows_size",section:"gRPC");
    s.GrpcMultiMode=Enabled(grpc,"multiMode"); s.GrpcAuthority=Text(grpc,"authority"); s.GrpcUserAgent=Text(grpc,"user_agent"); s.GrpcInitialWindow=Math.Max(0,Number(grpc,"initial_windows_size"));
    object mode=Value(grpc,"mode");
    if(mode!=null) {Need(mode is bool || (mode is string && new[]{"gun","multi"}.Contains((string)mode)),"Неподдерживаемый mode gRPC.");bool multi=mode is bool?(bool)mode:(string)mode=="multi";Need(Value(grpc,"multiMode")==null || s.GrpcMultiMode==multi,"Конфликт режимов gRPC.");s.GrpcMultiMode=multi;}
    s.ServiceName=Text(grpc,"serviceName"); s.GrpcIdleSeconds=Number(grpc,"idle_timeout"); s.GrpcPingSeconds=Number(grpc,"health_check_timeout"); s.GrpcPermitWithoutStream=Enabled(grpc,"permit_without_stream");
   }
   else Need(Child(stream,"grpcSettings").Count==0,"gRPC-параметры без gRPC.");
   if(network=="httpupgrade") {
    var upgrade=Child(stream,"httpupgradeSettings");Keys(upgrade,"host,path,headers,acceptProxyProtocol");Need(!Enabled(upgrade,"acceptProxyProtocol"),"PROXY protocol в клиентском HTTPUpgrade не поддерживается.");
    var headers=Child(upgrade,"headers");Need(headers.Count<=1 && headers.Keys.All(k=>k.Equals("Host",StringComparison.OrdinalIgnoreCase)),"Пользовательские заголовки HTTPUpgrade не поддерживаются.");
    s.TransportHost=Text(upgrade,"host");s.Path=Text(upgrade,"path");
    if(headers.Count>0) {Need(headers.Values.First() is string,"Неверный Host HTTPUpgrade.");string host=(string)headers.Values.First();Need(s.TransportHost=="" || s.TransportHost==host,"Конфликт Host HTTPUpgrade.");s.TransportHost=host;}
   } else Need(Child(stream,"httpupgradeSettings").Count==0,"Параметры HTTPUpgrade без HTTPUpgrade.");
   if(network=="xhttp") {
    Need(Value(stream,"xhttpSettings")==null || Value(stream,"splithttpSettings")==null,"Повторяющиеся настройки XHTTP.");
    var xhttp=Value(stream,"xhttpSettings")!=null?Child(stream,"xhttpSettings"):Child(stream,"splithttpSettings");
    const string tuning="xPaddingBytes,noGRPCHeader,scMaxEachPostBytes,scMinPostsIntervalMs,xmux,headers,noSSEHeader,scMaxBufferedPosts,scStreamUpServerSecs";
    var extra=Child(xhttp,"extra");
    Need(Value(xhttp,"downloadSettings")==null && Value(extra,"downloadSettings")==null,"XHTTP downloadSettings требует отдельного исходящего маршрута и пока не поддерживается.");
    Keys(xhttp,"host,path,mode,extra,"+tuning,section:"XHTTP"); Keys(extra,"path,"+tuning,section:"XHTTP extra");
    // Xray always takes the outer path, even when extra contains another path.
    extra.Remove("path");
    foreach(var option in extra) {
     Need(!xhttp.ContainsKey(option.Key),"Повторяющиеся параметры XHTTP.");
     xhttp.Add(option.Key,option.Value);
    }
    s.TransportHost=Text(xhttp,"host"); s.Path=Text(xhttp,"path"); s.XhttpMode=Text(xhttp,"mode","auto");
    s.XhttpPadding=Text(xhttp,"xPaddingBytes"); s.XhttpPostBytes=Text(xhttp,"scMaxEachPostBytes"); s.XhttpPostInterval=Text(xhttp,"scMinPostsIntervalMs"); s.XhttpNoGrpcHeader=Enabled(xhttp,"noGRPCHeader");
    Need(Child(xhttp,"headers").Count==0,"Пользовательские заголовки XHTTP пока не поддерживаются.");
    var muxOptions=Child(xhttp,"xmux");
    foreach(string key in muxOptions.Keys.OrderBy(k=>k,StringComparer.Ordinal)) s.XhttpMux.Add(key,Text(muxOptions,key));
   } else Need(Child(stream,"xhttpSettings").Count==0 && Child(stream,"splithttpSettings").Count==0,"Параметры XHTTP без XHTTP.");
   var tcp=Child(stream,"tcpSettings"); var raw=Child(stream,"rawSettings");
   foreach(var tcpSettings in new[]{tcp,raw}) {
    if(network!="tcp") Need(tcpSettings.Count==0,"TCP-параметры без TCP.");
    Keys(tcpSettings,"header"); var header=Child(tcpSettings,"header"); Keys(header,"type"); Need(Text(header,"type","none")=="none","Маскировка TCP не поддерживается.");
   }
   return s;
  }
 }
}

