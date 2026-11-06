using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Luna {
 internal static class LoginStartup {
  const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
  const string Name="doomra VPN";
  internal static string Command { get { return "\""+Assembly.GetExecutingAssembly().Location+"\" --autostart"; } }
  internal static bool Enabled {
   get {
    using(var key=Registry.CurrentUser.OpenSubKey(RunKey))
     return key!=null && string.Equals(key.GetValue(Name) as string,Command,StringComparison.OrdinalIgnoreCase);
   }
  }
  internal static void Set(bool enabled) {
   using(var key=Registry.CurrentUser.CreateSubKey(RunKey)) {
    if(enabled) key.SetValue(Name,Command);
    else if(string.Equals(key.GetValue(Name) as string,Command,StringComparison.OrdinalIgnoreCase)) key.DeleteValue(Name,false);
   }
  }
  internal static void RemoveFor(string sid) {
   using(var key=Registry.Users.OpenSubKey(sid+"\\"+RunKey,true))
    if(key!=null && string.Equals(key.GetValue(Name) as string,Command,StringComparison.OrdinalIgnoreCase)) key.DeleteValue(Name,false);
  }
 }

 public sealed partial class MainForm {
  static readonly int ShowMessage=RegisterWindowMessage("doomraVPN.Show.v1");
  NotifyIcon tray;
  ContextMenuStrip trayMenu;
  ToolStripMenuItem trayConnect;
  bool exitRequested,installed,launchAtLogin;

  internal static void RequestShow() { if(ShowMessage!=0) PostMessage(new IntPtr(0xffff),ShowMessage,IntPtr.Zero,IntPtr.Zero); }
  void InitializeTray(bool atLogin) {
   installed=VpnService.IsInstalled;
   launchAtLogin=installed && LoginStartup.Enabled;
   UpdateControls();
   trayMenu=new ContextMenuStrip();
   trayMenu.Items.Add("Открыть doomra VPN",null,delegate { ShowFromTray(); });
   trayConnect=new ToolStripMenuItem("Подключить",null,async delegate { await Toggle(); });
   trayMenu.Items.Add(trayConnect);
   trayMenu.Items.Add("Выход",null,delegate { exitRequested=true; Close(); });
   trayMenu.Opening+=delegate { trayConnect.Text=connection==null?"Подключить":"Отключить"; trayConnect.Enabled=connect.Enabled; };
   tray=new NotifyIcon { Text="doomra VPN",Icon=Icon,ContextMenuStrip=trayMenu,Visible=true };
   tray.DoubleClick+=delegate { ShowFromTray(); };
   if(atLogin && installed) Shown+=async delegate {
    Hide();
    if(launchAtLogin && settings.ConnectAtLogin && storageReady && Selected()!=null) await Toggle();
   };
  }
  void ShowFromTray() {
   if(closing) return;
   Show();
   if(WindowState==FormWindowState.Minimized) WindowState=FormWindowState.Normal;
   Activate();
  }
  void DisposeTray() {
   if(tray!=null) { tray.Visible=false; tray.Dispose(); }
   if(trayMenu!=null) trayMenu.Dispose();
  }
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int RegisterWindowMessage(string text);
  [DllImport("user32.dll")] static extern bool PostMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
 }
}
