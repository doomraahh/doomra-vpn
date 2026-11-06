using System;
using System.Linq;
using System.Windows.Forms;
namespace Luna {
 internal static class NetworkSettingsTests {
  internal static void Run(Action<string,Action> test,Action<bool> assert) {
   test("Network settings modes and kill toggle persist in transient form without starting VPN",delegate {
    var previous=System.Threading.SynchronizationContext.Current;
    try {
     var settings=new Settings(); using(var form=new MainForm(settings)) {
      var handle=form.Handle; var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
      Action<string> action=id=>form.GetType().GetMethod("RunAction",f).Invoke(form,new object[]{id});
      action("settings"); action("route-include"); assert(settings.Network.Mode=="include"); action("kill"); assert(settings.Network.KillSwitch);
      action("route-exclude"); assert(settings.Network.Mode=="exclude");
      assert(form.GetType().GetField("connection",f).GetValue(form)==null);
      var names=Enumerable.Range(0,form.AccessibilityObject.GetChildCount()).Select(i=>form.AccessibilityObject.GetChild(i).Name).ToList();
      assert(names.Contains("Kill switch") && names.Contains("Добавить приложение") && names.Contains("Восстановить интернет"));
      form.GetType().GetField("busy",f).SetValue(form,true); action("route-all"); assert(settings.Network.Mode=="exclude");
     }
    } finally {System.Threading.SynchronizationContext.SetSynchronizationContext(previous);}
   });
   test("Settings layout fits minimum window and app controls do not overlap",delegate {
    foreach(var size in new[]{new System.Drawing.Size(900,640),new System.Drawing.Size(1040,720)}) {
     var d=new DashboardLayout(size); var n=new NetworkSettingsLayout(d);
     assert(d.SettingsPage.Contains(n.Recovery) && d.SettingsPage.Contains(n.Kill));
     assert(!n.Modes[0].IntersectsWith(n.Modes[1]) && !n.Add.IntersectsWith(n.Rows[2]));
    }
   });
  }
 }
}
