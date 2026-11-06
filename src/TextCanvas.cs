using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Luna {
 internal static class TextCanvas {
  [DllImport("gdi32.dll")] static extern int SaveDC(IntPtr dc);
  [DllImport("gdi32.dll")] static extern bool RestoreDC(IntPtr dc,int saved);
  [DllImport("gdi32.dll")] static extern int SelectClipRgn(IntPtr dc,IntPtr region);
  [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr value);
  sealed class Context : IDeviceContext {
   readonly IntPtr handle;
   internal Context(IntPtr dc) { handle=dc; }
   public IntPtr GetHdc() { return handle; }
   public void ReleaseHdc() { }
   public void Dispose() { }
  }
  internal static void Draw(Graphics g,Region clip,string text,Font font,Rectangle bounds,Color color,TextFormatFlags flags) {
   IntPtr region=clip.GetHrgn(g),dc=IntPtr.Zero; int saved=0;
   try {
    dc=g.GetHdc(); saved=SaveDC(dc);
    if(saved==0 || SelectClipRgn(dc,region)==0) throw new InvalidOperationException("Unable to prepare text context.");
    using(var context=new Context(dc)) TextRenderer.DrawText(context,text,font,bounds,color,flags);
   } finally {
    if(dc!=IntPtr.Zero) { if(saved!=0) RestoreDC(dc,saved); g.ReleaseHdc(dc); }
    DeleteObject(region);
   }
  }
 }
}
