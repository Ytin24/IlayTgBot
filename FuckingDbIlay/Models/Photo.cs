using System;
using System.Collections.Generic;

namespace FuckingDbIlay.Models;

public partial class Photo
{
    public int Id { get; set; }

    public int? NoteId { get; set; }

    public string? Path { get; set; }

    public virtual Note? Note { get; set; }
}
