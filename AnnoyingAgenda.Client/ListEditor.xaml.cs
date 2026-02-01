using AnnoyingAgenda.Shared;
using System.IO;
using System.Windows.Controls;
using System.Text.Json;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media.Animation;

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
      InitializeComponent();

      var JsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda", "aa.json");

      if (!File.Exists(JsonFilePath) || string.IsNullOrWhiteSpace(File.ReadAllText(JsonFilePath)))
      {
        File.WriteAllText(JsonFilePath, JsonSerializer.Serialize(new List<ToDoList>()));
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
  }
}
