using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
namespace Luna {
 internal static class SubscriptionVisibilityTests {
  internal static void Run(Action<string,Action> test,Action<bool> assert) {
   test("Subscription reveal is explicit and resets on editor reopen",delegate {
    var context=System.Threading.SynchronizationContext.Current;
    try {using(var form=new MainForm(new Settings {Subscription="https://example.invalid/synthetic-token"})) {
     var handle=form.Handle;var flags=BindingFlags.Instance|BindingFlags.NonPublic;
     Action open=()=>form.GetType().GetMethod("ShowSubscription",flags).Invoke(form,null);
     open();var input=(TextBox)form.GetType().GetField("subscriptionInput",flags).GetValue(form);
     assert(input.PasswordChar!='\0');
     form.GetType().GetMethod("RunAction",flags).Invoke(form,new object[]{"toggle-subscription"});
     assert(input.PasswordChar=='\0');
     form.GetType().GetMethod("RunAction",flags).Invoke(form,new object[]{"toggle-subscription"});assert(input.PasswordChar!='\0');
     form.GetType().GetMethod("RunAction",flags).Invoke(form,new object[]{"toggle-subscription"});open();
     input=(TextBox)form.GetType().GetField("subscriptionInput",flags).GetValue(form);assert(input.PasswordChar!='\0');
    }} finally {System.Threading.SynchronizationContext.SetSynchronizationContext(context);}
   });
   test("Copy uses the actual edited subscription without revealing it or changing settings",delegate {
    var context=System.Threading.SynchronizationContext.Current;
    try {var settings=new Settings {Subscription="https://example.invalid/original"};using(var form=new MainForm(settings)) {
     var handle=form.Handle;var flags=BindingFlags.Instance|BindingFlags.NonPublic;
     form.GetType().GetMethod("ShowSubscription",flags).Invoke(form,null);
     var input=(TextBox)form.GetType().GetField("subscriptionInput",flags).GetValue(form);
     input.Text="https://example.invalid/edited-token";string copied=null;
     var copy=form.GetType().GetMethod("CopySubscription",flags);assert(copy!=null);
     copy.Invoke(form,new object[]{new Action<string>(s=>copied=s)});
     assert(copied==input.Text && input.PasswordChar!='\0' && settings.Subscription=="https://example.invalid/original");
     copy.Invoke(form,new object[]{new Action<string>(s=> {throw new Exception("private-token");})});
     var details=(DashboardDetails)form.GetType().GetField("details",flags).GetValue(form);
     assert(!details.Notice.Contains("private-token") && input.PasswordChar!='\0');
     input.Text="";copied=null;copy.Invoke(form,new object[]{new Action<string>(s=>copied=s)});assert(copied==null);
    }} finally {System.Threading.SynchronizationContext.SetSynchronizationContext(context);}
   });
  }
 }
}
