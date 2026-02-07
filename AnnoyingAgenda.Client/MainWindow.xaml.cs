using AnnoyingAgenda.Shared;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.ComponentModel;
using System.Security.Principal;

namespace AnnoyingAgenda.Client
{
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    private string _pageTitle;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string WindowTitle { get; set; }

    public string PageTitle
    {
      get => _pageTitle;
      set
      {
        if (_pageTitle == value) return;
        _pageTitle = value;
        OnPropertyChanged(nameof(PageTitle));
      }
    }

    public MainWindow()
    {
      InitializeComponent();
      MainNavigation.Navigate(new MainMenu(this));
      this.DataContext = this;
      WindowTitle = "Annoying Agenda";
      PageTitle = "Main Menu";

      var UserIdentity = WindowsIdentity.GetCurrent();
      var Principal = new WindowsPrincipal(UserIdentity);
      if (Principal.IsInRole(WindowsBuiltInRole.Administrator)) MainNavigation.Navigate(new SettingsPage(this));
    }

    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void DragWindow(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Left)
      {
        this.DragMove();
      }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      var JsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda", "Lists.json");
      var SettingsJsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda", "Settings.json");
      Settings ServiceSettings = new();

      if (!File.Exists(JsonFilePath) || string.IsNullOrWhiteSpace(File.ReadAllText(JsonFilePath)))
      {
        Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda"));
        File.WriteAllText(JsonFilePath, JsonSerializer.Serialize(new List<ToDoList>(), new JsonSerializerOptions() { WriteIndented = true }));
      }

      if (!File.Exists(SettingsJsonPath) || string.IsNullOrWhiteSpace(File.ReadAllText(SettingsJsonPath)))
      {
        Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda"));
        File.WriteAllText(SettingsJsonPath, JsonSerializer.Serialize(new Settings(), new JsonSerializerOptions() { WriteIndented = true }));
      }

      ServiceSettings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsJsonPath)) ?? new Settings();

      ServiceSettings.ClientRootPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
      ServiceSettings.ServiceRootPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "AnnoyingAgenda.Service.exe");

      File.WriteAllText(SettingsJsonPath, JsonSerializer.Serialize(ServiceSettings, new JsonSerializerOptions() { WriteIndented = true }));
    }

    private void CloseButton(object sender, RoutedEventArgs e)
    {
      Application.Current.Shutdown();
    }

    private void MinimizeButton(object sender, RoutedEventArgs e)
    {
      this.WindowState = WindowState.Minimized;
    }

    private void MaximizeButton(object sender, RoutedEventArgs e)
    {
      if (this.WindowState == WindowState.Normal)
      {
        this.WindowState = WindowState.Maximized;
        MaximizeBtn.Content = new Image { Source = new BitmapImage(new Uri("\\Assets\\Buttons\\AnnoyingAgenda-Button-Restore.png", UriKind.Relative)) };
      }
      else 
      {
        this.WindowState = WindowState.Normal;
        MaximizeBtn.Content = new Image { Source = new BitmapImage(new Uri("\\Assets\\Buttons\\AnnoyingAgenda-Button-Maximize.png", UriKind.Relative)) };
      }
    }
  }
}