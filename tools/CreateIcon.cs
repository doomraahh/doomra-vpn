using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Luna {
 static class CreateIcon {
  static void Main() {
   int[] sizes={16,32,48,64,128,256};
   var frames=new byte[sizes.Length][];
   var crop=new RectangleF(140,100,970,1060);
   using(var logo=Image.FromFile("assets/doomra-logo.png")) {
    for(int i=0;i<sizes.Length;i++) {
     int size=sizes[i];
     using(var frame=new Bitmap(size,size,PixelFormat.Format32bppArgb)) {
      using(var g=Graphics.FromImage(frame)) {
       g.CompositingMode=CompositingMode.SourceCopy;
       g.InterpolationMode=InterpolationMode.HighQualityBicubic;
       g.PixelOffsetMode=PixelOffsetMode.HighQuality;
       float height=size*.97f,width=height*crop.Width/crop.Height;
       var target=new RectangleF((size-width)/2,(size-height)/2,width,height);
       g.DrawImage(logo,target,crop,GraphicsUnit.Pixel);
      }
      using(var png=new MemoryStream()) {
       frame.Save(png,ImageFormat.Png);
       frames[i]=png.ToArray();
      }
     }
    }
   }
   using(var icon=new BinaryWriter(File.Create("assets/doomra.ico"))) {
    icon.Write((ushort)0);
    icon.Write((ushort)1);
    icon.Write((ushort)sizes.Length);
    int offset=6+16*sizes.Length;
    for(int i=0;i<sizes.Length;i++) {
     byte dimension=(byte)(sizes[i]==256?0:sizes[i]);
     icon.Write(dimension);
     icon.Write(dimension);
     icon.Write((ushort)0);
     icon.Write((ushort)1);
     icon.Write((ushort)32);
     icon.Write(frames[i].Length);
     icon.Write(offset);
     offset+=frames[i].Length;
    }
    foreach(var frame in frames) icon.Write(frame);
   }
  }
 }
}
