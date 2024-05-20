using System;
using System.Collections.Generic;

namespace DbIlay.Models;

public partial class Category
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<NotesCategory> NotesCategories { get; set; } = new List<NotesCategory>();
}
