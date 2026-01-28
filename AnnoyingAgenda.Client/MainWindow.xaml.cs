using AnnoyingAgenda.Shared;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace AnnoyingAgenda.Client
{
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
      MainNavigation.Navigate(new MainMenu());
    }

    private void DragWindow(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Left)
      {
        this.DragMove();
      }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      var JsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda", "aa.json");

      if (!File.Exists(JsonFilePath) || string.IsNullOrWhiteSpace(File.ReadAllText(JsonFilePath)))
      {
        File.WriteAllText(JsonFilePath, JsonSerializer.Serialize(new ToDoListCollection()));
      }
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