using System;
using System.Collections.Generic;

namespace BE.models;

public partial class CustomerGuest
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string? CarName { get; set; }

    public string? CarModel { get; set; }

    public string? LicensePlate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public long? BranchId { get; set; }

    public long? LinkedUserId { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual User? LinkedUser { get; set; }

    public virtual ICollection<ScheduleService> ScheduleServices { get; set; } = new List<ScheduleService>();
}
