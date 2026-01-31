using AnnoyingAgenda.Shared;
using System.Windows.Controls;

namespace AnnoyingAgenda.Client
{
  public partial class ListEditor : Page
  {
    private MainWindow ParentWindow;
    private ToDoList CurrentList;
    public ListEditor(MainWindow _parentWindow, ToDoList _currentList)
    {
      ParentWindow = _parentWindow;
      CurrentList = _currentList;
      InitializeComponent();
    }
  }
}
