using AnnoyingAgenda.Shared;
using System.IO;
using System.Windows.Controls;
using System.Text.Json;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Text.Json.Nodes;

namespace AnnoyingAgenda.Client
{
  public partial class ListEditor : Page
  {
    private MainWindow ParentWindow;
    private ToDoList CurrentList;
    private ToDoItem ChangingItem;
    private List<ToDoList> AllLists;

    private const string TaskDateSeparator = ", Due: ";
    private const string TaskDateFormat = "MM/dd/yyyy-HH:mm";

    private bool IsDeleting = false;
    private bool IsEditing = false;

    public ListEditor(MainWindow _parentWindow, ToDoList _currentList)
    {
      ParentWindow = _parentWindow;
      CurrentList = _currentList;
      ParentWindow.PageTitle = CurrentList.Name;
      InitializeComponent();
      MakeHours();
      MakeMinutes();

      EventDatePicker.DisplayDateStart = DateTime.Today;
      EventDatePicker.Text = DateTime.Today.ToString();

      var JsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda", "Lists.json");

      if (!File.Exists(JsonFilePath) || string.IsNullOrWhiteSpace(File.ReadAllText(JsonFilePath)))
      {
        File.WriteAllText(JsonFilePath, JsonSerializer.Serialize(new List<ToDoList>(), new JsonSerializerOptions(){ WriteIndented = true}));
      }
      else
      {
        JsonNode? ListNode = JsonNode.Parse(File.ReadAllText(JsonFilePath));
        AllLists = ListNode["AllLists"].Deserialize<List<ToDoList>>();
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
      EditHourSelector.ItemsSource = DisplayHours;
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
      EditMinuteSelector.ItemsSource = DisplayMinutes;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      try
      {
        if (CurrentList.ListItems.Count >= 1)
        {
          foreach (ToDoItem Item in CurrentList.ListItems)
          {
            Button TaskButton = CreateToDoButton(Item.Name, Item.DueDate);

            if (Item.IsComplete) TaskButton.Background = Brushes.LightGreen;

            TaskPanel.Children.Add(TaskButton);
          }
        }
      }
      catch (Exception)
      {
        MessageBox.Show("An error occurred when loading your to do list", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
      }
    }

    private void MouseOverAnimation(object sender, MouseEventArgs e)
    {
      Border HoveredButton = (Border)e.Source;

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
      Border HoveredButton = (Border)e.Source;

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

      if(QuitConfirmation == MessageBoxResult.Yes) ParentWindow.MainNavigation.Navigate(new ListPage(ParentWindow));
      else return;
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
      if (AllLists.Count >= 1)
      {
        ToDoList? OldCurrentList = AllLists.Find(L => L.Name == CurrentList.Name && L.Purpose == CurrentList.Purpose);
        int OldIndex = AllLists.FindIndex(L => L.Name == CurrentList.Name && L.Purpose == CurrentList.Purpose);

        AllLists.RemoveAt(OldIndex);
      }

      AllLists.Add(CurrentList);

      var JsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda", "Lists.json");

      JsonNode ListNode = JsonNode.Parse(File.ReadAllText(JsonFilePath));
      JsonArray ListArray = new();

      foreach(ToDoList List in AllLists)
      {
        ListArray.Add(List);
      }

      ListNode["AllLists"] = ListArray;

      File.WriteAllText(JsonFilePath, JsonSerializer.Serialize(ListNode, new JsonSerializerOptions() { WriteIndented = true }));
    }

    private void EventButtonClick(object sender, RoutedEventArgs e)
    {
      if (IsDeleting && !IsEditing)DeleteEvent(sender, e);
      else if (IsEditing && !IsDeleting)EditEvent(sender, e);
      else if(!IsDeleting && !IsEditing)MarkAsComplete(sender, e);
    }

    private void CreateEvent(object sender, RoutedEventArgs e)
    {
      string EventName = EventNameBox.Text;
      string EventDueHour = (string)HourSelector.SelectedItem;
      int EventDueMinute = MinuteSelector.SelectedIndex;

      if (string.IsNullOrWhiteSpace(EventName) || EventDatePicker.DisplayDate.ToString("MM/dd/yyyy") is null || HourSelector.SelectedItem is null || MinuteSelector.SelectedItem is null)
      {
        NewEventPopup.IsOpen = false;
        MessageBox.Show("Unable to create task", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        NewEventPopup.IsOpen = true;
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

      Button TaskButton = CreateToDoButton(EventName, EventDueDate);

      TaskPanel.Children.Add(TaskButton);
      NewEventPopup.IsOpen = false;
    }

    private void NewEventButton(object sender, RoutedEventArgs e) => NewEventPopup.IsOpen = true;
    private void SearchClick(object sender, RoutedEventArgs e) => SearchPopup.IsOpen = true;
    private void CancelNewEventClick(object sender, RoutedEventArgs e) => NewEventPopup.IsOpen = false;
    private void CancelEditClick(object sender, RoutedEventArgs e) => EditEventPopup.IsOpen = false;

    private void CancelSearchClick(object sender, RoutedEventArgs e)
    {
      SearchPopup.IsOpen = false;
      SearchBox.Text = string.Empty;
      SearchResultPanel.Children.Clear();
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

    private void ToggleEditMode(object sender, RoutedEventArgs e)
    {
      if (IsEditing)
      {
        EditButton.Foreground = Brushes.Black;
        IsEditing = false;
      }
      else
      {
        IsEditing = true;
        EditButton.Foreground = Brushes.Blue;
      }
    }

    private void DeleteEvent(object sender, RoutedEventArgs e) 
    {
      bool WasSearchOpen = SearchPopup.IsOpen;

      if (WasSearchOpen) SearchPopup.IsOpen = false;

      MessageBoxResult DeletionConfirmation = MessageBox.Show("This action deletes the task", "Confirm Deletion", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);

      if (WasSearchOpen) SearchPopup.IsOpen = true;
      if (DeletionConfirmation == MessageBoxResult.OK)
      {
        ToDoItem DeletedToDo = GetToDo(sender, e);
        Button DeletedTaskButton = GetToDoButton(DeletedToDo.Name, DeletedToDo.DueDate);

        TaskPanel.Children.Remove(DeletedTaskButton);
        CurrentList.ListItems.Remove(DeletedToDo);
      }
    }
    
    private void EditEvent(object sender, RoutedEventArgs e)
    {
      Button TaskButton = (Button)e.Source;
      ToDoItem ToDo = GetToDo(sender, e);

      ChangingItem = ToDo;

      EditNameBox.Text = ToDo.Name;
      EditDatePicker.SelectedDate = ToDo.DueDate.Date;
      EditHourSelector.Text = ToDo.DueDate.Hour.ToString("hh");
      EditMinuteSelector.Text = ToDo.DueDate.Minute.ToString("mm");

      EditEventPopup.IsOpen = true;
    }

    private void ConfirmEdit(object sender, RoutedEventArgs e)
    {
      ToDoItem EditedTodo = GetToDo(ChangingItem.Name, ChangingItem.DueDate);
      ToDoItem NewTodo = ChangingItem;

      Button EditedTaskButton = GetToDoButton(ChangingItem.Name, ChangingItem.DueDate);
      Button NewTaskButton = new();

      NewTodo.Name = EditNameBox.Text;
     

      if (string.IsNullOrWhiteSpace(NewTodo.Name) || EventDatePicker.DisplayDate.ToString("MM/dd/yyyy") is null || EditHourSelector.SelectedItem is null || EditMinuteSelector.SelectedItem is null)
      {
        MessageBox.Show("Unable to edit task", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      string EventDueHour = (string)EditHourSelector.SelectedItem;

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
     
      NewTodo.DueDate = EditDatePicker.DisplayDate.AddHours(int.Parse(EventDueHour)).AddMinutes(EditMinuteSelector.SelectedIndex);

      NewTaskButton = CreateToDoButton(NewTodo.Name, NewTodo.DueDate);

      CurrentList.ListItems.Remove(EditedTodo);
      CurrentList.ListItems.Add(NewTodo);

      TaskPanel.Children.Remove(EditedTaskButton);
      TaskPanel.Children.Add(NewTaskButton);

      EditEventPopup.IsOpen = false;
    }

    private void SearchForEvents(object sender, RoutedEventArgs e)
    {
      string SearchTerm = SearchBox.Text.ToLower();
      foreach(ToDoItem Item in CurrentList.ListItems)
      {
        if (Item.Name.ToLower().Contains(SearchTerm))
        {
          Button MatchingButton = CreateToDoButton(Item.Name, Item.DueDate);
          SearchResultPanel.Children.Add(MatchingButton);
        }
      }
    }

    private void MarkAsComplete(object sender, RoutedEventArgs e)
    {
      ToDoItem CompletedToDo = GetToDo(sender, e);
      Button TaskButton = (Button)e.Source;

      CompletedToDo.IsComplete = CompletedToDo.IsComplete ? false: true;
      TaskButton.Background = CompletedToDo.IsComplete ? Brushes.LightGreen : (Brush)new BrushConverter().ConvertFromString("#fbfbfb");
    }

    private Button CreateToDoButton(string name, DateTime date)
    {
      ToDoItem MatchingToDo = GetToDo(name, date);

      Button TaskButton = new()
      {
        Content = name + TaskDateSeparator + date.ToString(TaskDateFormat),
        Height = 40,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        FontSize = 20,
        FontFamily = new FontFamily("Segoe UI"),
        Foreground = (Brush) new BrushConverter().ConvertFromString("#292929"),
        Background = MatchingToDo.IsComplete? Brushes.LightGreen : (Brush)new BrushConverter().ConvertFromString("#fbfbfb"),
        Margin = new Thickness(0, 0, 0, 5),
        Style = (Style)this.FindResource("WindowButtonTriggers")
      };
      TaskButton.Click += EventButtonClick;
      return TaskButton;
    }

    private ToDoItem GetToDo(object sender, RoutedEventArgs e)
    {
      Button TaskButton = (Button)e.Source;

      string? ToDoName = TaskButton.Content.ToString().Split(TaskDateSeparator)[0];
      string? ToDoTime = TaskButton.Content.ToString().Split(TaskDateSeparator)[1];

      ToDoItem? Event = CurrentList.ListItems.Find(I => I.Name == ToDoName && I.DueDate.ToString(TaskDateFormat) == ToDoTime);

      return Event;
    }

    private Button GetToDoButton(string name, DateTime date)
    {
      ToDoItem Event = GetToDo(name, date);
      Button TaskButton = new();

      foreach(Button ListButton in TaskPanel.Children)
      {
        string ButtonContent = name + TaskDateSeparator + date.ToString(TaskDateFormat);
        if ((string)ListButton.Content == ButtonContent)
        {
          TaskButton = ListButton;
          break;
        }
      }
      return TaskButton;
    }

    private ToDoItem GetToDo(string name, DateTime date)
    {
      ToDoItem? Event = CurrentList.ListItems.Find(E => E.Name == name && E.DueDate.ToString(TaskDateFormat) == date.ToString(TaskDateFormat));
      return Event;
    }
  }
}