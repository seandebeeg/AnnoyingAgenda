using System;
using System.Collections.Generic;
using System.Text;

namespace AnnoyingAgenda.Shared
{
  class ToDoList
  {
    public required string Name { get; set; }
    public required string Purpose { get; set; }
  }

  class ToDoItem
  {
    public required string Name { get; set; }
    public DateTime DueDate {get; set;}
  }
}
