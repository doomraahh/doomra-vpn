using System;
using System.IO;
using System.Text;
namespace Luna {
 internal static class LocalSubscriptionImport {
  internal static ImportResult ParseText(string text) {
   try {return Subscriptions.Parse(text);}
   catch(SafeException e) {return new ImportResult {Errors=new System.Collections.Generic.List<string>{e.Message}};}
   catch {return new ImportResult {Errors=new System.Collections.Generic.List<string>{"Не удалось распознать содержимое подписки. Данные скрыты."}};}
  }
  internal static string ReadFile(string path) {
   try {
    string full=Path.GetFullPath(path);
    if(full.StartsWith(@"\\",StringComparison.Ordinal) || new DriveInfo(Path.GetPathRoot(full)).DriveType==DriveType.Network) throw new SafeException("Выберите файл на локальном диске.");
    byte[] bytes;
    using(var input=new FileStream(full,FileMode.Open,FileAccess.Read,FileShare.Read)) {
     if(input.Length>Subscriptions.Limit) throw new SafeException("Файл подписки превышает 4 МиБ.");
     using(var output=new MemoryStream()) {
      byte[] buffer=new byte[8192];int count;
      while((count=input.Read(buffer,0,buffer.Length))>0) {if(output.Length+count>Subscriptions.Limit) throw new SafeException("Файл подписки превышает 4 МиБ.");output.Write(buffer,0,count);}
      bytes=output.ToArray();
     }
    }
    try {
     Encoding encoding=new UTF8Encoding(false,true);int skip=0;
     if(bytes.Length>=4 && ((bytes[0]==0xff && bytes[1]==0xfe && bytes[2]==0 && bytes[3]==0) || (bytes[0]==0 && bytes[1]==0 && bytes[2]==0xfe && bytes[3]==0xff))) throw new SafeException("Сохраните файл как UTF-8 или UTF-16.");
     if(bytes.Length>=3 && bytes[0]==0xef && bytes[1]==0xbb && bytes[2]==0xbf) skip=3;
     else if(bytes.Length>=2 && bytes[0]==0xff && bytes[1]==0xfe) {encoding=new UnicodeEncoding(false,true,true);skip=2;}
     else if(bytes.Length>=2 && bytes[0]==0xfe && bytes[1]==0xff) {encoding=new UnicodeEncoding(true,true,true);skip=2;}
     string text=encoding.GetString(bytes,skip,bytes.Length-skip);
     if(Encoding.UTF8.GetByteCount(text)>Subscriptions.Limit) throw new SafeException("Текст подписки превышает 4 МиБ в UTF-8.");
     return text;
    } finally {Array.Clear(bytes,0,bytes.Length);}
   } catch(SafeException) {throw;}
   catch(DecoderFallbackException) {throw new SafeException("Не удалось прочитать кодировку. Сохраните файл как UTF-8 или UTF-16.");}
   catch {throw new SafeException("Не удалось открыть файл подписки. Проверьте доступ к локальному файлу.");}
  }
 }
}
