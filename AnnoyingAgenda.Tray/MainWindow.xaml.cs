using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.IO.Pipes;
namespace AnnoyingAgenda.Tray
{
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
      this.Hide();
    }
    private NamedPipeClientStream ClientPipe = new(".", "AnnoyingAgenda", PipeDirection.In);
  }
}