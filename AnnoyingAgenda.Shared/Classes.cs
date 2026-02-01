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
    }

    public string Name { get; set; }
    public DateTime DueDate {get; set;}
    public bool IsComplete { get; set; }
  }
}
