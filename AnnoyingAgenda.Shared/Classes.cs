namespace AnnoyingAgenda.Shared
{
  public class ToDoList
  {
    public ToDoList(string name, string purpose)
    {
      this.Name = name;
      this.Purpose = purpose;
      this.ListItems = new List<ToDoItem>();
    }

    public string Name { get; set; }
    public string Purpose { get; set; }
    public List<ToDoItem> ListItems {get; set;}
  }

  public class ToDoItem
  {
    public ToDoItem(string name, DateTime duedate)
    {
      Name = name;
      DueDate = duedate;
      IsComplete = false;
      TimesNotified = 0;
    }

    public string Name { get; set; }
    public DateTime DueDate {get; set;}
    public int TimesNotified { get; set; }
    public bool IsComplete { get; set; }
  }

  public class Settings
  {
    public string ClientRootPath { get; set; }
    public string ServiceRootPath { get; set; }
    public string TrayRootPath { get; set; }
    public bool IsServiceInstalled { get; set; }
    public List<SettingsItem> SettingsItems { get; set; }
  }

  public class SettingsItem : IEquatable<SettingsItem>
  {
    public required string Name { get; set; }
    public bool IsEnabled { get; set; }

    public bool Equals(SettingsItem? other)
    {
      if (other is null) return false;
      return Name == other.Name &&
        IsEnabled == other.IsEnabled;
    }

    public override bool Equals(object? obj)
    {
      if (obj is null) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != GetType()) return false;
      return Equals(obj as SettingsItem);
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Name, IsEnabled);
    }
  }
}
