using System;
using System.Collections.Generic;

namespace BE.models;

public partial class Branch
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public decimal? LaborRate { get; set; }

    public virtual ICollection<Car> Cars { get; set; } = new List<Car>();

    public virtual ICollection<Component> Components { get; set; } = new List<Component>();

    public virtual ICollection<CustomerGuest> CustomerGuests { get; set; } = new List<CustomerGuest>();

    public virtual ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();

    public virtual ICollection<ScheduleService> ScheduleServices { get; set; } = new List<ScheduleService>();

    public virtual ICollection<ServicePackage> ServicePackages { get; set; } = new List<ServicePackage>();

    public virtual ICollection<StockInRequest> StockInRequests { get; set; } = new List<StockInRequest>();

    public virtual ICollection<TicketComponent> TicketComponents { get; set; } = new List<TicketComponent>();

    public virtual ICollection<TotalReceipt> TotalReceipts { get; set; } = new List<TotalReceipt>();

    public virtual ICollection<TypeComponent> TypeComponents { get; set; } = new List<TypeComponent>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<VehicleCheckin> VehicleCheckins { get; set; } = new List<VehicleCheckin>();
}
