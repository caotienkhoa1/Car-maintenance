using System;
using System.Collections.Generic;

namespace BE.models;

public partial class TypeComponent
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public long? BranchId { get; set; }

    public string? StatusCode { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual ICollection<Component> Components { get; set; } = new List<Component>();

    public virtual StatusLookup? StatusCodeNavigation { get; set; }
}
