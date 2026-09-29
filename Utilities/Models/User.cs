using System;
using System.Collections.Generic;

namespace Utilities.Models;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string? Email { get; set; }
}
