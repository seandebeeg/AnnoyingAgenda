using AnnoyingAgenda.Shared;
using System.IO;
using System.Windows.Controls;
using System.Text.Json;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace AnnoyingAgenda.Client
{
  public partial class ListEditor : Page
  {
    private MainWindow ParentWindow;
    private ToDoList CurrentList;
    private List<ToDoList> AllLists;
    public ListEditor(MainWindow _parentWindow, ToDoList _currentList)
    {
      ParentWindow = _parentWindow;
      CurrentList = _currentList;
      ParentWindow.PageTitle = CurrentList.Name;
      InitializeComponent();

      var JsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda", "aa.json");

      if (!File.Exists(JsonFilePath) || string.IsNullOrWhiteSpace(File.ReadAllText(JsonFilePath)))
      {
        File.WriteAllText(JsonFilePath, JsonSerializer.Serialize(new List<ToDoList>(), new JsonSerializerOptions(){ WriteIndented = true}));
      }
      else
      {
        AllLists = JsonSerializer.Deserialize<List<ToDoList>>(File.ReadAllText(JsonFilePath));
        File.WriteAllText(JsonFilePath, JsonSerializer.Serialize(AllLists));
      }
    }

    private void MouseOverAnimation(object sender, MouseEventArgs e)
    {
      Button HoveredButton = (Button)e.Source;

      ThicknessAnimation LeftMarginAnimation = new()
      {
        From = new Thickness(0, 0, 0, 10),
        To = new Thickness(0, 0, 20, 10),
        Duration = TimeSpan.FromMilliseconds(100),
      };

      Storyboard.SetTargetProperty(LeftMarginAnimation, new PropertyPath("Margin"));
      Storyboard.SetTarget(LeftMarginAnimation, HoveredButton);
      Storyboard LeftMarginStoryBoard = new();

      LeftMarginStoryBoard.Children.Add(LeftMarginAnimation);
      LeftMarginStoryBoard.Begin();
    }

    private void MouseLeaveAnimation(object sender, MouseEventArgs e)
    {
      Button HoveredButton = (Button)e.Source;

      ThicknessAnimation LeftMarginAnimation = new()
      {
        From = new Thickness(0, 0, 20, 10),
        To = new Thickness(0, 0, 0, 10),
        Duration = TimeSpan.FromMilliseconds(100),
      };

      Storyboard.SetTargetProperty(LeftMarginAnimation, new PropertyPath("Margin"));
      Storyboard.SetTarget(LeftMarginAnimation, HoveredButton);
      Storyboard LeftMarginStoryBoard = new();

      LeftMarginStoryBoard.Children.Add(LeftMarginAnimation);
      LeftMarginStoryBoard.Begin();
    }

    private void QuitButton(object sender, RoutedEventArgs e)
    {
      MessageBoxResult QuitConfirmation = MessageBox.Show("Any unsaved work will be lost, do you want to proceed?", "Quit?", MessageBoxButton.YesNo, MessageBoxImage.Question);

      if(QuitConfirmation == MessageBoxResult.Yes)
      {
        ParentWindow.MainNavigation.Navigate(new ListPage(ParentWindow));
      }
      else
      {
        return;
      }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      try
      {
        if(CurrentList.ListItems.Count >= 1)
        {
          foreach (ToDoItem Item in CurrentList.ListItems)
          {
            Button TaskButton = new()
            {
              Content = Item.Name + " Due: " + Item.DueDate.ToString("MM/dd/yyyy-hh:mm"),
              Height = 40,
              HorizontalAlignment = HorizontalAlignment.Stretch,
              FontSize = 30,
              FontFamily = new FontFamily("Tw Cen MT Condensed"),
              Background = Brushes.LightGray,
              Margin = new Thickness(0,0,0,5),
              Style = (Style)this.FindResource("WindowButtonTriggers")
            };

            TaskPanel.Children.Add(TaskButton);
          }
        }
        else
        {
          TextBlock NoTasksMessage = new()
          {
            Text = "No Tasks :)",
            FontFamily = new FontFamily("Tw Cen MT Condensed"),
            FontSize = 35,
            HorizontalAlignment = HorizontalAlignment.Center
          };

          TaskPanel.Children.Add(NoTasksMessage);
        }
      }
      catch (Exception)
      {
        MessageBox.Show("An error occurred when loading your to do list","Error",MessageBoxButton.OK, MessageBoxImage.Exclamation);
      }
    }
  }
}
