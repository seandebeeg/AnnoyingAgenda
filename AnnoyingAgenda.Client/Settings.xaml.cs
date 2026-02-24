using AnnoyingAgenda.Shared;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Management.Automation;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AnnoyingAgenda.Client
{
  public partial class SettingsPage : Page
  {
    MainWindow ParentWindow;
    Settings? ServiceSettings = new();
    ObservableCollection<SettingsItem> SettingItems;

    public SettingsPage(MainWindow _parentWindow)
    {
      InitializeComponent();
      

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

      SettingItems = new ObservableCollection<SettingsItem>()
      {
        new SettingsItem() { Name = "Notification Bomb", IsEnabled = false},
        new SettingsItem() { Name = "Play Sounds", IsEnabled = false},
        new SettingsItem() { Name = "Close Apps", IsEnabled = false},
        new SettingsItem() { Name = "Spam Popups", IsEnabled = false}
      };

      var View = CollectionViewSource.GetDefaultView(SettingItems);
      View.GroupDescriptions.Add(new PropertyGroupDescription("Name"));
      SettingsList.ItemsSource = View;

      LoadSettings();
      UpdateInstallButton();
    }

    private bool IsServiceInstalled()
    {
      ServiceSettings.IsServiceInstalled = ServiceController.GetServices().Any(s => s.ServiceName == "AnnoyingAgendaService");
      SaveSettings();
      return ServiceSettings.IsServiceInstalled;
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
        var PSI = new ProcessStartInfo //UAC prompt to install service
        {
          Verb = "runas",
          UseShellExecute = true,
          FileName = Environment.ProcessPath
        };

        try
        {
          Process.Start(PSI);
          Application.Current.Shutdown();
          return;
        }
        catch(Exception)
        {
          MessageBox.Show("You need to be an administrator to install the service","Access Denied", MessageBoxButton.OK, MessageBoxImage.Hand);
          return;
        }
      }

     
      string ServicePath = ServiceSettings.ServiceRootPath;

      if (string.IsNullOrWhiteSpace(ServicePath))
      {
        MessageBox.Show("Service executable was not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      Process.Start("AnnoyingAgenda.Tray.exe");

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
        ServiceSettings.IsServiceInstalled = true;
        SaveSettings();
      }
      UpdateInstallButton();
    }

    private void UninstallService(object sender, RoutedEventArgs e)
    {
      if (!IsAdmin())
      {
        var PSI = new ProcessStartInfo //UAC prompt to uninstall service
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
        catch (Exception)
        {
          MessageBox.Show("You need to be an administrator to uninstall the service", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Hand);
          return;
        }
      }

      PowerShell PS = PowerShell.Create();

      PS.AddCommand("Stop-Service")
        .AddParameter("Name", "AnnoyingAgendaService")
        .Invoke();

      PS.Commands.Clear();

      PS.AddCommand("Remove-Service")
        .AddParameter("Name", "AnnoyingAgendaService")
        .Invoke();

      if (PS.HadErrors)
      {
        MessageBox.Show("Uninstall unsuccessful", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      else
      {
        MessageBox.Show("Uninstall successful", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        ServiceSettings.IsServiceInstalled = false;
        SaveSettings();
      }
      UpdateInstallButton();
    }

    private string GetServicePath()
    {
      try
      {
        ManagementObjectSearcher Searcher = new ManagementObjectSearcher("SELECT PathName FROM Win32_Service WHERE Name = '" + "AnnoyingAgendaService" + "'");
        foreach (ManagementObject Obj in Searcher.Get())
        {
          string PathName = Obj["PathName"] as string;
          if (!string.IsNullOrEmpty(PathName))
          {
            if (PathName.StartsWith("\""))
            {
              int EndQuoteIndex = PathName.IndexOf("\"", 1);
              if (EndQuoteIndex > 0)
              {
                string ExecutablePath = PathName.Substring(1, EndQuoteIndex - 1);
                return ExecutablePath;
              }
            }
            else
            {
              int FirstSpaceIndex = PathName.IndexOf(" ");
              if (FirstSpaceIndex > 0)
              {
                string ExecutablePath = PathName.Substring(0, FirstSpaceIndex);
                return ExecutablePath;
              }
              else
              {
                return PathName;
              }
            }
          }
        }
      }
      catch (ManagementException Ex)
      {
        Console.WriteLine("An error occurred when getting the service path: " + Ex.Message);
      }
      catch (Exception Ex)
      {
        Console.WriteLine("An error occurred: " + Ex.Message);
      }
      return null;
    }

    private void LoadSettings()
    {
      try
      {
        var SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda", "Settings.json");

        if (File.Exists(SettingsPath))
        {
          var LoadedSettings = JsonSerializer.Deserialize<Settings>(
          File.ReadAllText(SettingsPath));
         
          if (LoadedSettings?.SettingsItems != null)
          {
            foreach (var Item in SettingItems)
            {
              var SavedItem = LoadedSettings.SettingsItems
                .FirstOrDefault(I => I.Name == Item.Name);
              if (SavedItem != null)
              {
                Item.IsEnabled = SavedItem.IsEnabled;
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void SaveSettings(object sender, RoutedEventArgs e)
    {
      var SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda", "Settings.json");

      ServiceSettings.SettingsItems = SettingItems.Where(I => I.IsEnabled).ToList();

      File.WriteAllText(SettingsPath, JsonSerializer.Serialize(ServiceSettings, new JsonSerializerOptions { WriteIndented = true}));
    }

    private void SaveSettings()
    {
      var SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Annoying Agenda", "Settings.json");

      ServiceSettings.SettingsItems = SettingItems.Where(I => I.IsEnabled).ToList();

      File.WriteAllText(SettingsPath, JsonSerializer.Serialize(ServiceSettings, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void MainMenuClick(object sender, RoutedEventArgs e)
    {
      ParentWindow.MainNavigation.Navigate(new MainMenu(ParentWindow));
    }

  }
}
