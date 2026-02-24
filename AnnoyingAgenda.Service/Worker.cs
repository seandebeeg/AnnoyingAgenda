using AnnoyingAgenda.Shared;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;

namespace AnnoyingAgenda.Service
{
  public class Worker : BackgroundService
  {
    private readonly ILogger<Worker> _logger;
    
    private List<ToDoList> AllLists = new();
    private Settings ServiceSettings = new();
    private IOptionsMonitor<Settings> SettingsWatcher;
    private IOptionsMonitor<List<ToDoList>> ListWatcher;
    private object Sync = new();

    private NamedPipeServerStream ServicePipe = new( //Client side is AnnoyingAgenda.Tray
       "AnnoyingAgenda",
       PipeDirection.Out,
       1,
       PipeTransmissionMode.Message);

    public Worker(ILogger<Worker> logger, IOptionsMonitor<Settings> settingsMonitor, IOptionsMonitor<List<ToDoList>> listWatcher)
    {
      _logger = logger;
      SettingsWatcher = settingsMonitor;
      ListWatcher = listWatcher;

      SettingsWatcher.OnChange((Updated) => 
      {
        lock (Sync)
        {
          ServiceSettings = Updated;
          _logger.LogInformation("Settings Changed");
        }
      });

      ListWatcher.OnChange((Updated) => 
      {
        lock (Sync)
        {
          AllLists = Updated;
          _logger.LogInformation("Lists Changed");
        }
      });
      

      var SettingsJsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda", "Settings.json");

      if (!File.Exists(SettingsJsonPath) || string.IsNullOrWhiteSpace(File.ReadAllText(SettingsJsonPath)))
      {
        File.WriteAllText(SettingsJsonPath, JsonSerializer.Serialize(new Settings(), options: new JsonSerializerOptions() { WriteIndented = true }));
        ServiceSettings = new Settings();
      }
      else
      {
        ServiceSettings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsJsonPath)) ?? new Settings();
        File.WriteAllText(SettingsJsonPath, JsonSerializer.Serialize(ServiceSettings));
      }

      var ListFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda", "Lists.json");

      if (!File.Exists(ListFilePath) || string.IsNullOrWhiteSpace(File.ReadAllText(ListFilePath)))
      {
        File.WriteAllText(ListFilePath, JsonSerializer.Serialize(new List<ToDoList>(), new JsonSerializerOptions() { WriteIndented = true }));
      }

      ServiceSettings.ServiceRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnnoyingAgenda.Service.exe");
      ServiceSettings.TrayRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnnoyingAgenda.Tray.exe");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        lock (Sync)
        {
          foreach (ToDoList List in AllLists)
          {
            foreach (ToDoItem Item in List.ListItems)
            {
              if (DateTime.Now >= Item.DueDate && !Item.IsComplete)
              {
                _logger.LogInformation("Overdue Task: {Item}", Item.Name);
                
                if (Process.GetProcessesByName("AnnoyingAgenda.Tray").Length == 0)
                {
                  StartTrayApp();
                }

                if (!ServicePipe.IsConnected)
                {
                  ServicePipe.WaitForConnection();
                  _logger.LogInformation("Client Connection found");
                }

                NotifyUser(Item);
              }
            }
          }
        }
        await Task.Delay(3000, stoppingToken);
      }
    }

    private void StartTrayApp()
    {
      try
      {
        if (string.IsNullOrWhiteSpace(ServiceSettings.TrayRootPath) || !File.Exists(ServiceSettings.TrayRootPath))
        {
          _logger.LogError("Tray app path not found: {Path}", ServiceSettings.TrayRootPath);
          return;
        }

        var ProcessInfo = new ProcessStartInfo
        {
          FileName = ServiceSettings.TrayRootPath,
          Arguments = "--mode=hidden",
          UseShellExecute = true,
          RedirectStandardOutput = false,
          RedirectStandardError = false,
          CreateNoWindow = true
        };

        Process.Start(ProcessInfo);
        _logger.LogInformation("Tray app started: {Path}", ServiceSettings.TrayRootPath);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to start tray app");
      }
    }

    private async void NotifyUser(ToDoItem Item)
    {
      var Writer = new StreamWriter(ServicePipe) { AutoFlush = true };
      await Writer.WriteLineAsync($"{Item.Name} due on {Item.DueDate:MM-dd-yyyy hh:mm}");
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

      if (ServiceSettings.SettingsItems.Contains(ServiceSettings.SettingsItems.Find(I => I.Name == "Close Apps")))
        {
        try 
        {
          foreach(string AppName in ClosableApps)
          {
            Process[] AppProcesses = Process.GetProcessesByName(AppName);

            foreach(Process AppProcess in AppProcesses)
            {
              AppProcess.Kill();
              _logger.LogInformation("Closed App: {}", AppProcess.ProcessName);
            }
          }
        }
        catch (InvalidOperationException ex)
        {
          _logger.LogError("Couldn't close an app {errmsg}", ex);
        }
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
      int ChosenNumber = RandomNumber.Next(1, 10);
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
