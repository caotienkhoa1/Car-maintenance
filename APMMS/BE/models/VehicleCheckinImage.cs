using System;
using System.Collections.Generic;

namespace BE.models;

public partial class VehicleCheckinImage
{
    public long Id { get; set; }

    public long? VehicleCheckinId { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Description { get; set; }

    public string? Category { get; set; }

    public virtual VehicleCheckin? VehicleCheckin { get; set; }
}
