using AnnoyingAgenda.Shared;
using System.Text.Json;
using System.IO;
using System.Windows.Controls;

namespace AnnoyingAgenda.Client
{
  public partial class ListPage : Page
  {
    List<ToDoList>? AllLists = [];
    public ListPage()
    {
      InitializeComponent();

      var JsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda", "aa.json");

      if (!File.Exists(JsonFilePath) || string.IsNullOrWhiteSpace(File.ReadAllText(JsonFilePath)))
      {
        File.WriteAllText(JsonFilePath, JsonSerializer.Serialize(new List<ToDoList>()));
      }
      else
      {
        AllLists = JsonSerializer.Deserialize<List<ToDoList>>(File.ReadAllText(JsonFilePath));
      }
    }
  }
}
