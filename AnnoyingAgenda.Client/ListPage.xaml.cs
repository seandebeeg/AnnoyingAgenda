using AnnoyingAgenda.Shared;
using System.Text.Json;
using System.IO;
using System.Windows.Media;
using System.Windows;
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

        foreach(ToDoList List in AllLists)
        {
          Button ListSelectButton = new()
          {
            Content = List.Name + "\n" + List.Purpose,
            Height = 150,
            Width = 150,
            FontSize = 30,
            FontFamily = new FontFamily("Tw Cen MT Condensed"),
            Style = (Style)this.FindResource("WindowButtonTriggers"),
            Background = Brushes.LightGray,
            HorizontalAlignment = HorizontalAlignment.Center,
          };

          ListSelectPanel.Children.Add(ListSelectButton);
        }
      }
      }
    }
  }
}
