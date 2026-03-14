using AnnoyingAgenda.Shared;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using NAudio.Wave;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Forms;

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
   
    public Worker(ILogger<Worker> logger, IOptionsMonitor<Settings> settingsMonitor, IOptionsMonitor<List<ToDoList>> listWatcher)
    {
      RegistryKey? StartupRegistry = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

      if (StartupRegistry.GetValueNames().Contains("AnnoyingAgenda"))
      {
        StartupRegistry.SetValue("AnnoyingAgenda.Service", Path.Combine(Environment.ProcessPath, "AnnoyingAgenda.Service.exe"));
      }

      StartupRegistry.Close();

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

                Item.TimesNotified++;

                if (Item.TimesNotified >= 0) ExecuteNotificationLevel(1, Item);
                if (Item.TimesNotified >= 5) ExecuteNotificationLevel(2, Item);
                if (Item.TimesNotified >= 10) ExecuteNotificationLevel(3, Item);
                if (Item.TimesNotified >= 15) ExecuteNotificationLevel(4, Item);
              }
            }
          }
        }
        await Task.Delay(60000, stoppingToken);
      }
    }

    private void ExecuteNotificationLevel(int Level, ToDoItem Item)
    {
      if (Level == 1) SendToastNotification(Item);
      if (Level == 2) SpamMessageBoxes(Item); 
      if (Level == 3) PlaySound(ChooseSound()); 
      if (Level == 4) CloseDistractingApps();
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

    private void SpamMessageBoxes(ToDoItem Item)
    {
      for(int i = 0; i < Item.TimesNotified; i++)
      {
        MessageBox.Show("Overdue Task",
          $"{Item.Name} was due {Item.DueDate.ToString("MM/dd/yyyy HH:mm")}",
          MessageBoxButtons.OK,
          MessageBoxIcon.Exclamation);
      }
    }

    private void SendToastNotification(ToDoItem Item)
    {
      ToastContentBuilder Toast = new ToastContentBuilder()
        .AddText("Overdue Task")
        .AddText($"{Item.Name} was due {Item.DueDate.ToString("MM/dd/yyyy HH:mm")}")
        .SetToastScenario(ToastScenario.Reminder)
        .SetToastDuration(ToastDuration.Short);

      Toast.Show();
    }
  }
}
