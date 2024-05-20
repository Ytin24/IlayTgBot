using System;
using System.Collections.Generic;

namespace FuckingDbIlay.Models;

public partial class User
{
    public int Id { get; set; }

    public long? TelegramId { get; set; }

    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
}
