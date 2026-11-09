using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
namespace Luna {
 internal static class LocalImportTests {
  const string Uri="trojan://synthetic@example.invalid:443#Local";
  static Type Helper {get {return typeof(MainForm).Assembly.GetType("Luna.LocalSubscriptionImport",true);}}
  static object Call(string name,object value) {return Helper.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new[]{value});}
  internal static void Run(Action<string,Action> test,Action<bool> assert) {
   test("Local preview requires revalidation after editing and commits only the reviewed subset",delegate {
    var context=System.Threading.SynchronizationContext.Current;
    try {
    foreach(bool light in new[]{false,true}) {
     bool original=Palette.Light;Palette.Light=light;
     try {using(var dialog=new LocalImportDialog("Local",false)) {
      var handle=dialog.Handle;assert(!dialog.CanImport);
      dialog.SetContent(Uri+"\n"+Uri+"\ninvalid-private-token");dialog.ValidateContent();assert(dialog.CanImport);
      dialog.SetContent(Uri+"\n");assert(!dialog.CanImport);dialog.Commit();assert(dialog.Result==null);
      dialog.ValidateContent();dialog.Commit();assert(dialog.Result.Servers.Count==1 && dialog.ProfileName=="Local");
     }} finally {Palette.Light=original;}
    }
    } finally {System.Threading.SynchronizationContext.SetSynchronizationContext(context);}
   });
   test("Local parser enforces UTF8 byte limit and reports partial counts without raw values",delegate {
    var result=(ImportResult)Call("ParseText",Uri+"\n"+Uri+"\nunknown://private-token@example.invalid");
    assert(result.Servers.Count==1 && result.DuplicateCount==1 && result.Errors.Count==1 && !string.Join(" ",result.Errors).Contains("private-token"));
    var large=(ImportResult)Call("ParseText",new string('я',Subscriptions.Limit/2+1));
    assert(large.Servers.Count==0 && large.Errors.Count>0);
   });
   test("Local text import reuses URI Base64 JSON and YAML parsing without downloading URLs",delegate {
    foreach(string text in new[]{Uri,Convert.ToBase64String(Encoding.UTF8.GetBytes(Uri)),"{\"outbounds\":[{\"protocol\":\"trojan\",\"settings\":{\"address\":\"example.invalid\",\"port\":443,\"password\":\"synthetic\"},\"streamSettings\":{\"security\":\"tls\"}}]}","proxies:\n  - name: Local\n    type: trojan\n    server: example.invalid\n    port: 443\n    password: synthetic\n"}) {
     var result=(ImportResult)Call("ParseText",text);assert(result.Servers.Count==1 && result.Errors.Count==0);
    }
    var rejected=(ImportResult)Call("ParseText","https://example.invalid/private-token");
    assert(rejected.Servers.Count==0 && rejected.Errors.Count>0 && !string.Join(" ",rejected.Errors).Contains("private-token"));
   });
   test("Local files handle BOM and reject oversized or malformed UTF8 without paths in errors",delegate {
    string folder=Path.Combine(Path.GetTempPath(),"doomra-local-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
    string path=Path.Combine(folder,"private-token.txt");
    try {
     foreach(var encoding in new Encoding[]{new UTF8Encoding(true),Encoding.Unicode}) {
      File.WriteAllText(path,Uri,encoding);assert((string)Call("ReadFile",path)==Uri);
     }
     File.WriteAllBytes(path,new byte[]{0xc3,0x28});bool bad=false;
     try {Call("ReadFile",path);} catch(TargetInvocationException e) {bad=e.InnerException is SafeException && !e.InnerException.Message.Contains("private-token");} assert(bad);
     using(var file=File.Create(path)) file.SetLength(Subscriptions.Limit+1);
     bool large=false;try {Call("ReadFile",path);} catch(TargetInvocationException e) {large=e.InnerException is SafeException;} assert(large);
    } finally {File.Delete(path);Directory.Delete(folder);}
   });
   test("Editor exposes file and text import and local commit disables network refresh",delegate {
    var previous=System.Threading.SynchronizationContext.Current;
    try {using(var form=new MainForm(new Settings())) {
     var handle=form.Handle;var flags=BindingFlags.Instance|BindingFlags.NonPublic;
     form.GetType().GetMethod("ShowEditor",flags).Invoke(form,new object[]{null});
     var names=Enumerable.Range(0,form.AccessibilityObject.GetChildCount()).Select(i=>form.AccessibilityObject.GetChild(i).Name).ToArray();
     assert(names.Contains("Импорт из файла") && names.Contains("Вставить текст подписки"));
     var imported=Subscriptions.Parse(Uri+"\n"+Uri);
     var task=(System.Threading.Tasks.Task)form.GetType().GetMethod("ApplyLocalImport",flags).Invoke(form,new object[]{"Локальный",imported});
     assert(task.IsCompleted);task.GetAwaiter().GetResult();
     var settings=(Settings)form.GetType().GetField("settings",flags).GetValue(form);
     var profile=ProfileStore.Current(settings);
     assert(profile.Subscription=="" && profile.DirectUrl=="" && profile.Servers.Count==1);
     assert((bool)typeof(SubscriptionProfile).GetField("LocalImport").GetValue(profile));
     assert(!(bool)form.GetType().GetMethod("EnabledAction",flags).Invoke(form,new object[]{"refresh-profile"}));
     form.GetType().GetMethod("ShowEditor",flags).Invoke(form,new object[]{profile.Id});
     form.Controls.OfType<TextBox>().First(t=>t.PasswordChar=='\0').Text="Переименован";
     ((System.Threading.Tasks.Task)form.GetType().GetMethod("AddSubscription",flags).Invoke(form,null)).GetAwaiter().GetResult();
     var copy=Json.Read<Settings>(Json.Write(settings));ProfileStore.Initialize(copy);
     assert(ProfileStore.Current(copy).Name=="Переименован" && copy.Servers.Count==1);
    }} finally {System.Threading.SynchronizationContext.SetSynchronizationContext(previous);}
   });
  }
 }
}
