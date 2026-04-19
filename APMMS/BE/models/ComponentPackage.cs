using System;
using System.Collections.Generic;

namespace BE.models;

public partial class ComponentPackage
{
    public long ComponentId { get; set; }

    public long ServicePackageId { get; set; }

    public int Quantity { get; set; }

    public virtual Component Component { get; set; } = null!;

    public virtual ServicePackage ServicePackage { get; set; } = null!;
}
