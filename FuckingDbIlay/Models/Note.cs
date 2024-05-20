using System;
using System.Collections.Generic;

namespace DbIlay.Models;

public partial class Note
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<NotesCategory> NotesCategories { get; set; } = new List<NotesCategory>();

    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

    public virtual ICollection<ToDoList> ToDoLists { get; set; } = new List<ToDoList>();

    public virtual User? User { get; set; }
}
