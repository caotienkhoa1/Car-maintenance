using System;
using System.Collections.Generic;

namespace BE.models;

public partial class StatusLookup
{
    public string Code { get; set; } = null!;

    public string? Name { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Component> Components { get; set; } = new List<Component>();

    public virtual ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();

    public virtual ICollection<ScheduleService> ScheduleServices { get; set; } = new List<ScheduleService>();

    public virtual ICollection<ServiceCategory> ServiceCategories { get; set; } = new List<ServiceCategory>();

    public virtual ICollection<ServicePackage> ServicePackages { get; set; } = new List<ServicePackage>();

    public virtual ICollection<ServiceTask> ServiceTasks { get; set; } = new List<ServiceTask>();

    public virtual ICollection<StockInRequest> StockInRequests { get; set; } = new List<StockInRequest>();

    public virtual ICollection<StockIn> StockIns { get; set; } = new List<StockIn>();

    public virtual ICollection<TotalReceipt> TotalReceipts { get; set; } = new List<TotalReceipt>();

    public virtual ICollection<TypeComponent> TypeComponents { get; set; } = new List<TypeComponent>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<VehicleCheckin> VehicleCheckins { get; set; } = new List<VehicleCheckin>();
}
