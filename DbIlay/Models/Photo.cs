using System;
using System.Collections.Generic;

namespace DbIlay.Models;

public partial class Photo
{
    public int Id { get; set; }

    public int? NoteId { get; set; }

    public string? Path { get; set; }

    public virtual Note? Note { get; set; }
}
