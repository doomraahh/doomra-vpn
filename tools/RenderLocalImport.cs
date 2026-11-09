using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
namespace Luna {
 static class RenderLocalImport {
  [STAThread] static int Main(string[] args) {
   if(args.Length<1) return 1;
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   Palette.Light=Array.IndexOf(args,"--light")>=0;
   using(var dialog=new LocalImportDialog("Личный профиль",false)) {
    if(Array.IndexOf(args,"--small")>=0) dialog.Size=dialog.MinimumSize;
    dialog.SetContent("trojan://synthetic@example.invalid:443#Первый\r\ntrojan://synthetic@example.invalid:443#Повтор\r\nvless://11111111-1111-4111-8111-111111111111@node.invalid:443#Второй\r\ninvalid-example");
    dialog.ValidateContent();Create(dialog);
    using(var image=new Bitmap(dialog.Width,dialog.Height)) {dialog.DrawToBitmap(image,new Rectangle(Point.Empty,dialog.Size));image.Save(args[0],ImageFormat.Png);}
   }
   return 0;
  }
  static void Create(Control control) {var handle=control.Handle;control.PerformLayout();foreach(Control child in control.Controls) Create(child);}
 }
}
