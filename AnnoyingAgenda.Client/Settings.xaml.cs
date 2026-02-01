using System.Windows.Controls;

namespace AnnoyingAgenda.Client
{
  public partial class SettingsPage : Page
  {
    MainWindow ParentWindow;
    public SettingsPage(MainWindow _parentWindow)
    {
      InitializeComponent();

      ParentWindow = _parentWindow;
      ParentWindow.PageTitle = "Settings";
    }
  }
}
