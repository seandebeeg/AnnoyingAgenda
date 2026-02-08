using AnnoyingAgenda.Shared;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AnnoyingAgenda.Service
{
  public class Worker : BackgroundService
  {
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
      _logger = logger;
      _cancellationToken = cancellationToken.ApplicationStopping;

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
      else
      {
        AllLists = JsonSerializer.Deserialize<List<ToDoList>>(File.ReadAllText(ListFilePath)) ?? new List<ToDoList>();
      }

      ServiceSettings.ServiceRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnnoyingAgenda.Service.exe");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      bool HasOverdueTasks = false;

      while (!stoppingToken.IsCancellationRequested)
      {
        foreach(ToDoList List in AllLists)
        {
          foreach(ToDoItem Item in List.ListItems)
          {
            if (DateTime.Now >= Item.DueDate)
            {
              HasOverdueTasks = true;
              _logger.LogInformation("Overdue Task: {Item}", Item.Name);
            }
          }
        }

        if (HasOverdueTasks)
        {

        }
        await Task.Run(() => UpdateListsAndSettings());
        await Task.Delay(3000);
      }
    }
        {
          _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        }
        await Task.Delay(1000, stoppingToken);
      }
    }
  }
}
