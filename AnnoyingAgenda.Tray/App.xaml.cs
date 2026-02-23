using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.IO.Pipes;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace AnnoyingAgenda.Tray
{
  public partial class App : System.Windows.Application
  {
    private NotifyIcon Tray = new();

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      Tray = new();

      Tray.Icon = Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
      Tray.Visible = true;
      Tray.DoubleClick += (s, args) => Process.Start("AnnoyingAgenda.Client.exe"); 
    }

    NamedPipeClientStream ClientPipe = new(".", "AnnoyingAgenda", PipeDirection.In);
  }
}
