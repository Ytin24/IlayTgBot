using System;
using System.Collections.Generic;

namespace FuckingDbIlay.Models;

public partial class ToDoList
{
    public int Id { get; set; }

    public int? NoteId { get; set; }

    public string? Task { get; set; }

    public bool? Completed { get; set; }

    public virtual Note? Note { get; set; }
}
