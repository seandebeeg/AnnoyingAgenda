using AnnoyingAgenda.Shared;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

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
      ParentWindow.PageTitle = "Lists";

      var JsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda", "Lists.json");
      

      if (!File.Exists(JsonFilePath) || string.IsNullOrWhiteSpace(File.ReadAllText(JsonFilePath)))
      {
        File.WriteAllText(JsonFilePath, JsonSerializer.Serialize(new List<ToDoList>(), new JsonSerializerOptions() { WriteIndented = true }));
      }
      else
      {
        JsonNode? ListNode = JsonNode.Parse(File.ReadAllText(JsonFilePath));

        AllLists = ListNode["AllLists"].Deserialize<List<ToDoList>>();

        foreach (ToDoList List in AllLists)
        {
          Border ListSelectButtonBorder = new()
          {
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(5),
            BorderBrush = Brushes.DarkGray,
            Margin = new Thickness(0, 0, 0, 5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
          };

          Button ListSelectButton = new()
          {
            Content = List.Name + " - " + List.Purpose,
            Height = 60,
            FontSize = 30,
            FontFamily = new FontFamily("Tw Cen MT Condensed"),
            Style = (Style)this.FindResource("WindowButtonTriggers"),
            Background = Brushes.LightGray,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
          };

          ListSelectButton.Click += OpenList;
          ListSelectButtonBorder.Child = ListSelectButton;

          ListSelectPanel.Children.Add(ListSelectButtonBorder);
        }
      }
    }

    private void OpenList(object sender, RoutedEventArgs e)
    {
      Button ListButton = (Button)e.Source;

      string NameAndPurpose = (string)ListButton.Content;
      string Name = NameAndPurpose.Split(" - ")[0];

      ToDoList SelectedList = AllLists.Find(L => L.Name == Name);

      ParentWindow.MainNavigation.Navigate(new ListEditor(ParentWindow, SelectedList));
    }

    private void NewListButton(object sender, RoutedEventArgs e)
    {
      NewListPopup.IsOpen = true;
    }

    private void CancelPopupButton(object sender, RoutedEventArgs e)
    {
      NewListPopup.IsOpen = false;
    }

    private void HomeMenuButton(object sender, RoutedEventArgs e)
    {
      ParentWindow.MainNavigation.Navigate(new MainMenu(ParentWindow));
    }

    private void CreatePopupButton(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(ListName.Text) || string.IsNullOrEmpty(ListPurpose.Text))
      {
        MessageBox.Show("Name or Purpose cannot be empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      else
      {
        ToDoList NewToDoList = new ToDoList(ListName.Text, ListPurpose.Text);
        ParentWindow.MainNavigation.Navigate(new ListEditor(ParentWindow, NewToDoList));
      }
    }
  }
}