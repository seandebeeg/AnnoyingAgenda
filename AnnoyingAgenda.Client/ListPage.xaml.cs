using AnnoyingAgenda.Shared;
using System.Text.Json;
using System.IO;
using System.Windows.Controls;

namespace AnnoyingAgenda.Client
{
  public partial class ListPage : Page
  {
    private MainWindow ParentWindow;
    private List<ToDoList>? AllLists = [];
    public ListPage(MainWindow _parentWindow)
    {
      InitializeComponent();

      ParentWindow = _parentWindow;

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
