using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Text.Json;
using AnnoyingAgenda.Shared;

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
      if (this.WindowState == WindowState.Normal) this.WindowState = WindowState.Maximized;
      else this.WindowState = WindowState.Normal;
    }
  }
}