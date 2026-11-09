using System;
using System.Drawing;
using System.Windows.Forms;
namespace Luna {
 internal sealed class LocalImportDialog : Form {
  readonly TextBox name=new TextBox(),content=new TextBox();
  readonly Label summary=new Label();
  readonly Button import=new Button(),validate=new Button(),file=new Button(),cancel=new Button();
  readonly bool active;
  readonly Font dialogFont=new Font("Segoe UI",10),textFont=new Font("Consolas",10);
  bool fontsDisposed;
  ImportResult preview;
  internal ImportResult Result {get;private set;}
  internal string ProfileName {get;private set;}
  internal LocalImportDialog(string profileName,bool activeConnection) {
   active=activeConnection;
   Text="Импорт подписки";StartPosition=FormStartPosition.CenterParent;ShowInTaskbar=false;MinimizeBox=false;MaximizeBox=false;
   AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(660,520);MinimumSize=new Size(560,460);
   BackColor=Palette.Surface;ForeColor=Palette.Text;Font=dialogFont;
   var table=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=7};
   table.RowStyles.Add(new RowStyle(SizeType.Absolute,25));table.RowStyles.Add(new RowStyle(SizeType.Absolute,36));
   table.RowStyles.Add(new RowStyle(SizeType.Absolute,33));table.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   table.RowStyles.Add(new RowStyle(SizeType.Absolute,64));table.RowStyles.Add(new RowStyle(SizeType.Absolute,35));table.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
   table.Controls.Add(new Label {Text="Название профиля",Dock=DockStyle.Fill},0,0);
   name.Text=profileName;name.MaxLength=48;name.AccessibleName="Название импортируемого профиля";name.Dock=DockStyle.Fill;table.Controls.Add(name,0,1);
   table.Controls.Add(new Label {Text="URI, Base64, Xray JSON или Clash YAML · до 4 МиБ",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,2);
   content.Multiline=true;content.AcceptsReturn=true;content.AcceptsTab=true;content.WordWrap=false;content.ScrollBars=ScrollBars.Both;
   content.MaxLength=Subscriptions.Limit;content.Dock=DockStyle.Fill;content.AccessibleName="Текст подписки";content.Font=textFont;
   content.TextChanged+=delegate {preview=null;import.Enabled=false;summary.Text="Нажмите «Проверить», чтобы увидеть состав подписки.";};
   table.Controls.Add(content,0,3);summary.Dock=DockStyle.Fill;summary.Padding=new Padding(0,8,0,0);summary.Text="Вставьте текст или выберите файл.";table.Controls.Add(summary,0,4);
   table.Controls.Add(new Label {Text=active?"Импорт сменит профиль и отключит текущий VPN.":"Профиль сохранится локально. Обновление — повторным импортом.",Dock=DockStyle.Fill},0,5);
   var actions=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false};
   ConfigureButton(cancel,"Отмена");cancel.DialogResult=DialogResult.Cancel;
   ConfigureButton(import,"Импортировать");import.Enabled=false;import.Click+=delegate {Commit();};
   ConfigureButton(validate,"Проверить");validate.Click+=delegate {ValidateContent();};
   ConfigureButton(file,"Из файла…");file.Click+=delegate {ChooseFile();};
   actions.Controls.AddRange(new Control[]{cancel,import,validate,file});table.Controls.Add(actions,0,6);Controls.Add(table);CancelButton=cancel;
   foreach(var input in new[]{name,content}) {input.BackColor=Palette.Input;input.ForeColor=Palette.Text;input.BorderStyle=BorderStyle.FixedSingle;}
   Shown+=delegate {content.Focus();};
  }
  void ConfigureButton(Button button,string text) {button.Text=text;button.AutoSize=true;button.Height=32;button.FlatStyle=FlatStyle.Flat;button.BackColor=Palette.SurfaceBottom;button.ForeColor=Palette.Text;button.FlatAppearance.BorderColor=Palette.Border;}
  internal bool ChooseFile() {
   using(var picker=new OpenFileDialog {Title="Выберите файл подписки",Filter="Подписки (*.txt;*.json;*.yaml;*.yml;*.conf)|*.txt;*.json;*.yaml;*.yml;*.conf|Все файлы (*.*)|*.*",CheckFileExists=true,Multiselect=false,RestoreDirectory=true}) {
    if(picker.ShowDialog(this)!=DialogResult.OK) return false;
    try {content.Text=LocalSubscriptionImport.ReadFile(picker.FileName);ValidateContent();return true;}
    catch(SafeException e) {preview=null;import.Enabled=false;summary.Text=e.Message;return false;}
   }
  }
  internal void SetContent(string text) {content.Text=text;}
  internal bool CanImport {get {return import.Enabled;}}
  internal void ValidateContent() {
   preview=LocalSubscriptionImport.ParseText(content.Text);
   summary.Text="Серверов: "+preview.Servers.Count+". Отклонено: "+preview.Errors.Count+". Повторов: "+preview.DuplicateCount+".";
   if(preview.Servers.Count==0) summary.Text+=" "+(preview.Errors.Count>0?preview.Errors[0]:"Поддерживаемые серверы не найдены.");
   else if(preview.Errors.Count>0) summary.Text+=" Будут импортированы только распознанные серверы.";
   import.Enabled=preview.Servers.Count>0;
  }
  internal void Commit() {
   if(!CanImport || preview==null) return;
   try {ProfileName=ProfileStore.CleanName(name.Text);} catch(SafeException e) {summary.Text=e.Message;name.Focus();return;}
   Result=preview;DialogResult=DialogResult.OK;
  }
  protected override void Dispose(bool disposing) {base.Dispose(disposing);if(disposing && !fontsDisposed) {fontsDisposed=true;textFont.Dispose();dialogFont.Dispose();}}
 }
}
