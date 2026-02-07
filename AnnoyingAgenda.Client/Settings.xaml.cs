using AnnoyingAgenda.Shared;
using System.IO;
using System.ServiceProcess;
using System.Text.Json;
using System.Windows;
using System.Management.Automation;
using System.Windows.Controls;
using System.Security.Principal;
using System.Diagnostics;

namespace AnnoyingAgenda.Client
{
  public partial class SettingsPage : Page
  {
    MainWindow ParentWindow;
    Settings? ServiceSettings = new();

    public SettingsPage(MainWindow _parentWindow)
    {
      InitializeComponent();
      UpdateInstallButton();

      ParentWindow = _parentWindow;
      ParentWindow.PageTitle = "Settings";

      var SettingsJsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda", "Settings.json");

      if (!File.Exists(SettingsJsonPath) || string.IsNullOrWhiteSpace(File.ReadAllText(SettingsJsonPath)))
      {
        File.WriteAllText(SettingsJsonPath, JsonSerializer.Serialize(new Settings(), new JsonSerializerOptions() { WriteIndented = true }));
        ServiceSettings = new Settings();
      }
      else
      {
        ServiceSettings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsJsonPath));
        File.WriteAllText(SettingsJsonPath, JsonSerializer.Serialize(ServiceSettings));
      }


    }

    private bool IsServiceInstalled()
    {
      return ServiceController.GetServices().Any(s => s.ServiceName == "AnnoyingAgendaService");
    }

    private bool IsAdmin()
    {
      var UserIdentity = WindowsIdentity.GetCurrent();
      var Principal = new WindowsPrincipal(UserIdentity);
      return Principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private void UpdateInstallButton()
    {
      bool ServiceInstalled = IsServiceInstalled();

      if (ServiceInstalled) { 
        InstallationButton.Content = "Uninstall Service";
        InstallationButton.Click -= InstallService;
        InstallationButton.Click += UninstallService;
      }
      else
      {
        InstallationButton.Content = "Install Service";
        InstallationButton.Click -= UninstallService;
        InstallationButton.Click += InstallService;
      }
    }
    
    private void InstallService(object sender, RoutedEventArgs e)
    {
      if (!IsAdmin())
      {
        var PSI = new ProcessStartInfo
        {
          Verb = "runas",
          UseShellExecute = true,
          FileName = Environment.ProcessPath
        };

        try
        {
          Process.Start(PSI);
          Application.Current.Shutdown();
        }
        catch(Exception ex)
        {
          MessageBox.Show("You need to be an administrator to install the service","Access Denied", MessageBoxButton.OK, MessageBoxImage.Hand);
          return;
        }
      }

      try
      {
        string ServicePath = ServiceSettings.ServiceRootPath;

        if (string.IsNullOrWhiteSpace(ServicePath))
        {
          MessageBox.Show("Service executable was not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        PowerShell PS = PowerShell.Create();

        PS.AddCommand("New-Service")
          .AddParameter("Name", "AnnoyingAgendaService")
          .AddParameter("BinaryPath", ServicePath)
          .AddParameter("DisplayName", "Annoying Agenda Service")
          .AddParameter("StartupType", "Automatic")
          .Invoke();

        if (PS.HadErrors)
        {
          MessageBox.Show($"An error occurred when installing {string.Join("\n", PS.Streams.Error.Select(e => e.Exception.Message))}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        PS.Commands.Clear();
        PS.AddCommand("Start-Service")
          .AddParameter("Name", "AnnoyingAgendaService")
          .Invoke();

        if (PS.HadErrors)
        {
          MessageBox.Show($"An error occurred when starting the service {string.Join("\n", PS.Streams.Error.Select(e=> e.Exception.Message))}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }
        else
        {
          MessageBox.Show("The service has successfully installed and started", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
      }
      catch (Exception)
      {

      }
    }

    private void UninstallService(object sender, RoutedEventArgs e)
    {

    }

    private string GetServicePath()
    {
      string ServicePath = Path.Combine(ServiceSettings.ServiceRootPath, "AnnoyingAgenda.Service.exe");

      if (!string.IsNullOrWhiteSpace(ServicePath) && File.Exists(ServicePath))
      {
        return ServicePath;
      }

      return null;
    }

    private void MainMenuClick(object sender, RoutedEventArgs e)
    {
      ParentWindow.MainNavigation.Navigate(new MainMenu(ParentWindow));
    }

  }
}
