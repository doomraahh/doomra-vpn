using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Luna {
 internal sealed class ResizeFrame : IDisposable {
  readonly Size size;
  IntPtr bitmap,dc,previous;

  internal ResizeFrame(Bitmap image) {
   size=image.Size;
   bitmap=image.GetHbitmap();
   dc=CreateCompatibleDC(IntPtr.Zero);
   if(dc!=IntPtr.Zero) previous=SelectObject(dc,bitmap);
  }

  internal bool Draw(Graphics graphics,Size target) {
   if(previous==IntPtr.Zero || previous==new IntPtr(-1)) return false;
   IntPtr output=graphics.GetHdc();
   try {
    int oldMode=SetStretchBltMode(output,3); // COLORONCOLOR
    try { return StretchBlt(output,0,0,target.Width,target.Height,dc,0,0,size.Width,size.Height,0x00cc0020); }
    finally { if(oldMode!=0) SetStretchBltMode(output,oldMode); }
   } finally { graphics.ReleaseHdc(output); }
  }

  public void Dispose() {
   if(dc!=IntPtr.Zero) {
    if(previous!=IntPtr.Zero && previous!=new IntPtr(-1)) SelectObject(dc,previous);
    DeleteDC(dc);
    dc=previous=IntPtr.Zero;
   }
   if(bitmap!=IntPtr.Zero) { DeleteObject(bitmap); bitmap=IntPtr.Zero; }
  }

  [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
  [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
  [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
  [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
  [DllImport("gdi32.dll")] static extern int SetStretchBltMode(IntPtr dc,int mode);
  [DllImport("gdi32.dll")] static extern bool StretchBlt(IntPtr output,int x,int y,int width,int height,IntPtr input,int srcX,int srcY,int srcWidth,int srcHeight,uint rasterOperation);
 }
}
