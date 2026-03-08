using Microsoft.Win32;
using NAudio.Wave;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Toolkit.Uwp.Notifications;

namespace AnnoyingAgenda.Tray
{
  public partial class App : System.Windows.Application
  {
    private NotifyIcon Tray = new();

    NamedPipeClientStream ClientPipe = new(
      ".",
      "AnnoyingAgenda",
      PipeDirection.In); //AnnoyingAgenda.Service is server side

    private App()
    {
      RegistryKey StartupRegistry = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

      if (StartupRegistry.GetValueNames().Contains("AnnoyingAgenda"))
      {
        StartupRegistry.SetValue("AnnoyingAgenda.Tray", System.Windows.Forms.Application.ExecutablePath);
      }

      StartupRegistry.Close();

      ClientPipe.Connect();

      var Reader = new StreamReader(ClientPipe);
      string? ServiceMessage = Reader.ReadLine();

      while (ClientPipe.IsConnected)
      {
        ServiceMessage = Reader.ReadLine();

        if (!string.IsNullOrEmpty(ServiceMessage))
        {
          if (ServiceMessage == "Close Apps") CloseDistractingApps();
          else if (ServiceMessage == "Play Sound") PlaySound(ChooseSound());
          else if (ServiceMessage.Contains("Message Box:"))
          {
            System.Windows.MessageBox.Show(
              ServiceMessage.Remove(0, "Message Box:".Length),
              "Overdue Task",
              MessageBoxButton.OK,
              MessageBoxImage.Hand);
          }
          else if (ServiceMessage.Contains("Toast Notification:"))
          {
            new ToastContentBuilder()
              .AddText("Annnoying Agenda")
              .AddText(ServiceMessage.Remove(0, "Toast Notification:".Length))
              .SetToastScenario(ToastScenario.Reminder)
              .SetToastDuration(ToastDuration.Short)
              .Show();
          }
        }
      }

      App.Current.Shutdown();
    } 
    

    private void CloseDistractingApps()
    {
      string[] ClosableApps = [
        "chrome", "chatgpt", "Discord",
        "minecraft.windows", "Minecraft", "opera",
        "firefox", "steam", "tiktok",
        "instagram", "XboxPcApp", "whatsapp",
        "hulu", "prime", "disney",
        "tubi", "crunchyroll", "paramount",
        "espn", "netflix", "roblox", "javaw"
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
      Debug.WriteLine(FileName);

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

      FileName = ChosenNumber switch
      {
        1 => "america-eagle-gunshots.mp3",
        2 => "and-his-name-is-john-cena-1_3.mp3",
        3 => "door-knocking-very-realistic.mp3",
        4 => "eas-sound.mp3",
        5 => "hl2-stalker-scream.mp3",
        6 => "loud-explosion.mp3",
        7 => "loud-incorrect-buzzer.mp3",
        8 => "modern-warfare-2-tactical-nuke-sound.mp3",
        9 => "nuclear-diarrhea.mp3",
        10 => "windows-11-error-sound.mp3",
        _ => "windows-11-error-sound.mp3"
      };

      return FileName;
    }
  }
}
