using AnnoyingAgenda.Shared;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

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
   

    private static NamedPipeServerStream ServicePipe = new( //Client side is AnnoyingAgenda.Tray
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
                  _logger.LogInformation("Client Connection Successful");
                }

                if (Item.TimesNotified >= 0) ExecuteNotificationLevel(1, Item);
                if (Item.TimesNotified >= 5) ExecuteNotificationLevel(2, Item);
                if (Item.TimesNotified >= 10) ExecuteNotificationLevel(3, Item);
                if (Item.TimesNotified >= 15) ExecuteNotificationLevel(4, Item);
                

                Debug.WriteLine(Item.TimesNotified);
              }
            }
          }
        }
        await Task.Delay(60000, stoppingToken);
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

    private async void ExecuteNotificationLevel(int Level, ToDoItem Item)
    {
      Item.TimesNotified++;

      if (Level == 1)
      {
        await SendToast(Item);
      }
      else if (Level == 2)
      {
        
        await SendMessageBox(Item);
      }
      else if (Level == 3)
      {
       
        await SendSoundRequest();
      }
      else if (Level == 4)
      {
       
        await RequestAppClosure();
      }
    }

    private async Task SendMessageBox(ToDoItem Item)
    {
      if (!ServicePipe.IsMessageComplete) ServicePipe.WaitForPipeDrain();

      if (!ServiceSettings.SettingsItems.Contains(new SettingsItem
      {
        Name = "Popup Spam",
        IsEnabled = true
      })) return;

      var Writer = new StreamWriter(ServicePipe) { AutoFlush = true };
      await Writer.WriteLineAsync($"Message Box Notification:{Item.Name} due on {Item.DueDate:MM-dd-yyyy hh:mm}");
    }

    private async Task SendSoundRequest()
    {
      if (!ServicePipe.IsMessageComplete) ServicePipe.WaitForPipeDrain();

      if (!ServiceSettings.SettingsItems.Contains(new SettingsItem
      {
        Name = "Play Sounds",
        IsEnabled = true
      })) return;

      var Writer = new StreamWriter(ServicePipe) { AutoFlush = true };
      await Writer.WriteLineAsync($"Play Sound");
    }

    private async Task RequestAppClosure()
    {
      if (!ServicePipe.IsMessageComplete) ServicePipe.WaitForPipeDrain();

      if (!ServiceSettings.SettingsItems.Contains(new SettingsItem
      {
        Name = "Close Apps",
        IsEnabled = true
      })) return;

      var Writer = new StreamWriter(ServicePipe) { AutoFlush = true };
      await Writer.WriteLineAsync($"Close Apps");
    }

    private async Task SendToast(ToDoItem Item)
    {
      if (!ServicePipe.IsMessageComplete) ServicePipe.WaitForPipeDrain();

      if (!ServiceSettings.SettingsItems.Contains(new SettingsItem
      {
        Name = "Notification Bomb",
        IsEnabled = true
      })) return;

      var Writer = new StreamWriter(ServicePipe) { AutoFlush = true };
      await Writer.WriteLineAsync($"Toast Notification:{Item.Name} due on {Item.DueDate:MM-dd-yyyy hh:mm}");
    }
  }
}
