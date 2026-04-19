using System;
using System.Collections.Generic;

namespace BE.models;

public partial class ServiceCategory
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? StatusCode { get; set; }

    public decimal? StandardLaborTime { get; set; }

    public virtual ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();

    public virtual ICollection<ScheduleService> ScheduleServices { get; set; } = new List<ScheduleService>();

    public virtual ICollection<ServicePackageCategory> ServicePackageCategories { get; set; } = new List<ServicePackageCategory>();

    public virtual ICollection<ServiceTask> ServiceTasks { get; set; } = new List<ServiceTask>();

    public virtual StatusLookup? StatusCodeNavigation { get; set; }
}
