using AnnoyingAgenda.Shared;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AnnoyingAgenda.Service
{
  public class Worker : BackgroundService
  {
    private readonly ILogger<Worker> _logger;
    private CancellationToken CurrentCancellationToken;
    private IConfiguration Configuration;
    private List<ToDoList> AllLists = [];
    private Settings ServiceSettings = new();
    private IOptionsMonitor<Settings> SettingsWatcher;
    private IOptionsMonitor<List<ToDoList>> ListWatcher;
    private object Sync = new();

    public Worker(ILogger<Worker> logger, IOptionsMonitor<Settings> settingsMonitor, IOptionsMonitor<List<ToDoList>> listWatcher, IConfiguration configuration)
    {
      _logger = logger;
      SettingsWatcher = settingsMonitor;
      Configuration = configuration;
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
      CurrentCancellationToken = stoppingToken;
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
            }
          }
        }

        if (HasOverdueTasks)
        {

        }

        await Task.Delay(3000);
      }
    }
  }
}
