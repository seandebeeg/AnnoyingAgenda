using Microsoft.Win32;
using NAudio.Wave;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Windows;
using System.Windows.Forms;

namespace AnnoyingAgenda.Tray
{
  public partial class App : System.Windows.Application
  {
    private NotifyIcon Tray = new();

    NamedPipeClientStream ClientPipe = new(".",
      "AnnoyingAgenda",
      PipeDirection.In); //AnnoyingAgenda.Service is server side

    protected override async void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      RegistryKey StartupRegistry = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

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
          if (ServiceMessage == "Close Apps")
          {
            CloseApps();
          }
          else if (ServiceMessage == "Play Sound")
          {
            PlaySound(ChooseSound());
          }
          else
          {
            System.Windows.MessageBox.Show(ServiceMessage, "Overdue Task", MessageBoxButton.OK, MessageBoxImage.Hand);
          }
        }
      }

      App.Current.Shutdown();
    }

    private void CloseApps()
    {
      string[] ClosableApps = [
        "chrome", "chatgpt", "Discord",
        "minecraft.windows", "Minecraft", "opera",
        "firefox", "steam", "tiktok",
        "instagram", "XboxPcApp", "whatsapp",
        "hulu","prime","disney",
        "tubi","crunchyroll","paramount",
        "espn","netflix", "roblox", "javaw"
      ];

      try
      {
        foreach (string AppName in ClosableApps)
        {
          Process[] AppProcesses = Process.GetProcessesByName(AppName);

          foreach (Process AppProcess in AppProcesses)
          {
            AppProcess.Kill(); 
          }
        }
      }
      catch (InvalidOperationException)
      {
        return;
      }
    }

    private void PlaySound(string FileName)
    {
      AudioFileReader Reader = new(Path.Combine("Assets", "Sounds", FileName));
      WaveOutEvent Player = new();

      Player.Init(Reader);
      Player.Play();

      while (Player.PlaybackState == PlaybackState.Playing)
      {
        Task.Delay(100).Wait();
      }
    }

    private string ChooseSound()
    {
      Random RandomNumber = new();
      int ChosenNumber = RandomNumber.Next(1, 11);
      string FileName = string.Empty;

      switch (ChosenNumber)
      {
        case 1:
          FileName = "america-eagle-gunshots.mp3";
          break;
        case 2:
          FileName = "and-his-name-is-john-cena-1_3.mp3";
          break;
        case 3:
          FileName = "door-knocking-very-realistic.mp3";
          break;
        case 4:
          FileName = "eas-sound.mp3";
          break;
        case 5:
          FileName = "hl2-stalker-scream.mp3";
          break;
        case 6:
          FileName = "loud-explosion.mp3";
          break;
        case 7:
          FileName = "loud-incorrect-buzzer.mp3";
          break;
        case 8:
          FileName = "modern-warfare-2-tactical-nuke-sound.mp3";
          break;
        case 9:
          FileName = "nuclear-diarrhea.mp3";
          break;
        case 10:
          FileName = "windows-11-error-sound.mp3";
          break;
      }
      return FileName;
    }
  }
}
