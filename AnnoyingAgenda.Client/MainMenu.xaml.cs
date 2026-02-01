using System.Windows;
using System.Windows.Controls;

namespace AnnoyingAgenda.Client
{
  public partial class MainMenu : Page
  {
    MainWindow ParentWindow;
    public MainMenu(MainWindow _parentWindow)
    {
      ParentWindow = _parentWindow;
      ParentWindow.PageTitle = "Main Menu";
      InitializeComponent();
    }

    private void ListsClick(object sender, RoutedEventArgs e)
    {
      ParentWindow.MainNavigation.Navigate(new ListPage(ParentWindow));
    }

    private void SettingsClick(object sender, RoutedEventArgs e)
    {
      ParentWindow.MainNavigation.Navigate(new SettingsPage(ParentWindow));
    }
  }
}
