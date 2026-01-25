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
  }
}