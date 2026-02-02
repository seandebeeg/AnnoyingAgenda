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
    private bool IsDeleting = false;


    public ListEditor(MainWindow _parentWindow, ToDoList _currentList)
    {
      ParentWindow = _parentWindow;
      CurrentList = _currentList;
      ParentWindow.PageTitle = CurrentList.Name;
      InitializeComponent();
      MakeHours();
      MakeMinutes();

      EventDatePicker.DisplayDateStart = DateTime.Today;
      EventDatePicker.DisplayDate = DateTime.Today;

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

    private void MakeHours()
    {
      List<DateTime> HoursList = new();
      List<string> DisplayHours = new();
      for(int i = 0; i < 25; i++)
      {
        HoursList.Add(DateTime.Today.AddHours(i));
        if (i >= 12)
        {
          DisplayHours.Add(HoursList[i].ToString("hh") + "PM");
        }
        else
        {
          DisplayHours.Add(HoursList[i].ToString("hh") + "AM");
        }
      }
      HourSelector.ItemsSource = DisplayHours;
    }

    private void MakeMinutes()
    {
      List<DateTime> MinutesList = new();
      List<string> DisplayMinutes = new();
      for (int i = 0; i < 60; i++)
      {
        MinutesList.Add(DateTime.Today.AddMinutes(i));
        DisplayMinutes.Add(MinutesList[i].ToString("mm"));
      }
      MinuteSelector.ItemsSource = DisplayMinutes;
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

    private void EventButtonClick(object sender, RoutedEventArgs e)
    {
      if (IsDeleting)
      {
        MessageBoxResult DeletionConfirmation = MessageBox.Show("This action deletes the task", "Confirm Deletion", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
        if (DeletionConfirmation == MessageBoxResult.OK)
        {
          Button DeletedTaskButton = (Button)e.Source;
          TaskPanel.Children.Remove(DeletedTaskButton);
          string DeletedToDoName = DeletedTaskButton.Content.ToString().Split(", Due: ")[0];
          string DeletedToDoTime = DeletedTaskButton.Content.ToString().Split(", Due: ")[1];
          ToDoItem DeletedToDo = CurrentList.ListItems.Find(I => I.Name == DeletedToDoName && I.DueDate.ToString("MM/dd/yyyy-HH:mm") == DeletedToDoTime);
          CurrentList.ListItems.Remove(DeletedToDo);
        }
      }
      else return;
    }

    private void CreateEvent(object sender, RoutedEventArgs e)
    {
      string EventName = EventNameBox.Text;
      string EventDueHour = (string)HourSelector.SelectedItem;
      int EventDueMinute = MinuteSelector.SelectedIndex;

      if (string.IsNullOrWhiteSpace(EventName) || EventDatePicker.DisplayDate.ToString("MM/dd/yyyy") is null || HourSelector.SelectedItem is null || MinuteSelector.SelectedItem is null)
      {
        MessageBox.Show("Unable to create task", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      if (EventDueHour.Contains("AM"))
      {
        EventDueHour = EventDueHour.Replace("AM", "");
      }
      else
      {
        EventDueHour = EventDueHour.Replace("PM", "");
        int EventDueHourNumber = int.Parse(EventDueHour);
        EventDueHourNumber += 12;
        EventDueHour = EventDueHourNumber.ToString();
        if (int.Parse(EventDueHour) == 24) EventDueHour = 0.ToString();
      }
      DateTime EventDueDate = EventDatePicker.DisplayDate.AddHours(int.Parse(EventDueHour)).AddMinutes(MinuteSelector.SelectedIndex);

     

      ToDoItem Event = new(EventName, EventDueDate);
      CurrentList.ListItems.Add(Event);

      Button TaskButton = new()
      {
        Content = EventName + ", Due: " + EventDueDate.ToString("MM/dd/yyyy-HH:mm"),
        Height = 40,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        FontSize = 30,
        FontFamily = new FontFamily("Tw Cen MT Condensed"),
        Background = Brushes.LightGray,
        Margin = new Thickness(0, 0, 0, 5),
        Style = (Style)this.FindResource("WindowButtonTriggers")
      };
      TaskButton.Click += EventButtonClick;
      TaskPanel.Children.Add(TaskButton);
      NewEventPopup.IsOpen = false;
    }

    private void CreateNewEvent(object sender, RoutedEventArgs e)
    {
      NewEventPopup.IsOpen = true;
    }

    private void CancelNewEventClick(object sender, RoutedEventArgs e)
    {
      NewEventPopup.IsOpen = false;
    }

    private void ToggleDeletionMode(object sender, RoutedEventArgs e)
    {
      if (IsDeleting) 
      {
        DeleteButton.Foreground = Brushes.Black;
        IsDeleting = false;
      }
      else 
      { 
        IsDeleting = true;
        DeleteButton.Foreground = Brushes.Red;
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
              Content = Item.Name + " Due: " + Item.DueDate.ToString("MM/dd/yyyy-HH:mm"),
              Height = 40,
              HorizontalAlignment = HorizontalAlignment.Stretch,
              FontSize = 30,
              FontFamily = new FontFamily("Tw Cen MT Condensed"),
              Background = Brushes.LightGray,
              Margin = new Thickness(0,0,0,5),
              Style = (Style)this.FindResource("WindowButtonTriggers")
            };
            TaskButton.Click += EventButtonClick;
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