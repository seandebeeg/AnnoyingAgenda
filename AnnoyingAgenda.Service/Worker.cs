using AnnoyingAgenda.Shared;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Uwp.Notifications;
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
        File.WriteAllText(SettingsJsonPath, JsonSerializer.Serialize(new Settings(), new JsonSerializerOptions() { WriteIndented = true }));
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
      bool HasOverdueTasks = false;

      while (!stoppingToken.IsCancellationRequested)
      {
        foreach (ToDoList List in AllLists)
        {
          foreach (ToDoItem Item in List.ListItems)
          {
            if (DateTime.Now >= Item.DueDate && !Item.IsComplete)
            {
              HasOverdueTasks = true;
              _logger.LogInformation("Overdue Task: {Item}", Item.Name);
              CloseApps();  
            }
          }
        }
        await Task.Delay(3000);
          }
        }

    private void CloseApps()
    {
      string[] ClosableApps = [
        "chrome", "chatgpt", "Discord",
        "minecraft.windows", "Minecraft", "opera",
        "firefox", "steam", "tiktok",
        "instagram", "XboxPcApp","whatsapp",
        "hulu","prime","disney",
        "tubi","crunchyroll","paramount",
        "espn","netflix", "roblox"];

      if (true)
        {
        try 
        {
          foreach(string AppName in ClosableApps)
          {
            Process[] AppProcesses = Process.GetProcessesByName(AppName);

            foreach(Process AppProcess in AppProcesses)
            {
              AppProcess.CloseMainWindow();
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
  }
}
