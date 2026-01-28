using System;
using System.Collections.Generic;
using System.Text;

namespace AnnoyingAgenda.Shared
{
  class ToDoListCollection
  {
    public List<ToDoList>? AllToDoLists { get; set; }
  }
  class ToDoList
  {
    public required string Name { get; set; }
    public required string Purpose { get; set; }
    public required List<ToDoItem> ListItems {get; set;}
  }

  class ToDoItem
  {
    public required string Name { get; set; }
    public DateTime DueDate {get; set;}
    public bool IsComplete { get; set; }
  }
}
