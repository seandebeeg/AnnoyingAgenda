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
                  _logger.LogInformation("Client Connection Successful");
                }

                if (Item.TimesNotified >= 0 
                  && ServiceSettings.SettingsItems.Contains(new SettingsItem { Name = "Notification Bomb", IsEnabled = true }))
                {
                  Item.TimesNotified++;
                  SendToast(Item);
                }

                if (Item.TimesNotified >= 5
                  && ServiceSettings.SettingsItems.Contains(new SettingsItem { Name = "Popup Spam", IsEnabled = true }))
                {
                  Item.TimesNotified++;
                  SendMessageBox(Item);
                }

                if (Item.TimesNotified >= 10
                  && ServiceSettings.SettingsItems.Contains(new SettingsItem { Name = "Play Sounds", IsEnabled = true }))
                {
                  Item.TimesNotified++;
                  SendSoundRequest();
                }

                if (Item.TimesNotified >= 15
                  && ServiceSettings.SettingsItems.Contains(new SettingsItem { Name = "Close Apps", IsEnabled = true }))
                {
                  Item.TimesNotified++;
                  RequestAppClosure();
                }

                Debug.WriteLine(Item.TimesNotified);
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

    private async void SendMessageBox(ToDoItem Item)
    {
      var Writer = new StreamWriter(ServicePipe) { AutoFlush = true };
      await Writer.WriteLineAsync($"Message Box Notification:{Item.Name} due on {Item.DueDate:MM-dd-yyyy hh:mm}");
    }

    private async void SendSoundRequest()
    {
      var Writer = new StreamWriter(ServicePipe) { AutoFlush = true };
      await Writer.WriteLineAsync($"Play Sound");
    }

    private async void RequestAppClosure()
    {
      var Writer = new StreamWriter(ServicePipe) { AutoFlush = true };
      await Writer.WriteLineAsync($"Close Apps");
    }

    private async void SendToast(ToDoItem Item)
    {
      var Writer = new StreamWriter(ServicePipe) { AutoFlush = true };
      await Writer.WriteLineAsync($"Toast Notification:{Item.Name} due on {Item.DueDate:MM-dd-yyyy hh:mm}");
    }
  }
}
