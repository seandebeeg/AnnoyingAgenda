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
