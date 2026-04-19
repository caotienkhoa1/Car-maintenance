using System;
using System.Collections.Generic;

namespace BE.models;

public partial class ServicePackageCategory
{
    public long Id { get; set; }

    public long ServicePackageId { get; set; }

    public long ServiceCategoryId { get; set; }

    public decimal? StandardLaborTime { get; set; }

    public virtual ServiceCategory ServiceCategory { get; set; } = null!;

    public virtual ServicePackage ServicePackage { get; set; } = null!;
}
