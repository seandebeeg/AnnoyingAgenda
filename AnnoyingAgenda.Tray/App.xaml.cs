using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.IO.Pipes;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.IO;

namespace AnnoyingAgenda.Tray
{
  public partial class App : System.Windows.Application
  {
    private NotifyIcon Tray = new();

    NamedPipeClientStream ClientPipe = new(".", "AnnoyingAgenda", PipeDirection.In); //AnnoyingAgenda.Service is server side

    protected override async void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      RegistryKey StartupRegistry = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVErsion\\Run", true);

      StartupRegistry.SetValue("AnnoyingAgenda.Tray", System.Windows.Forms.Application.ExecutablePath);

      Tray = new();

      Tray.Icon = Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
      Tray.Visible = true;
      Tray.DoubleClick += (s, args) => Process.Start("AnnoyingAgenda.Client.exe");

      await ClientPipe.ConnectAsync();

      var Reader = new StreamReader(ClientPipe);
      string ServiceMessage = await Reader.ReadLineAsync();

      while(ClientPipe.IsConnected)
      {
        ServiceMessage = await Reader.ReadLineAsync();

        if (!string.IsNullOrEmpty(ServiceMessage))
        {
          System.Windows.MessageBox.Show(ServiceMessage, "Overdue Task", MessageBoxButton.OK, MessageBoxImage.Stop);
        }
      }

      App.Current.Shutdown();
    }
  }
}
