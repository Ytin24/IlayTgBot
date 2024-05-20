using System;
using System.Collections.Generic;

namespace FuckingDbIlay.Models;

public partial class NotesCategory
{
    public int Id { get; set; }

    public int? NoteId { get; set; }

    public int? CategoryId { get; set; }

    public virtual Category? Category { get; set; }

    public virtual Note? Note { get; set; }
}
