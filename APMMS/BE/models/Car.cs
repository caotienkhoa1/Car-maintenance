using System;
using System.Collections.Generic;

namespace BE.models;

public partial class Car
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public string? CarName { get; set; }

    public string? CarModel { get; set; }

    public long? VehicleTypeId { get; set; }

    public string? Color { get; set; }

    public string? LicensePlate { get; set; }

    public string? VehicleEngineNumber { get; set; }

    public string? VinNumber { get; set; }

    public int? YearOfManufacture { get; set; }

    public long? BranchId { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();

    public virtual ICollection<ScheduleService> ScheduleServices { get; set; } = new List<ScheduleService>();

    public virtual ICollection<TotalReceipt> TotalReceipts { get; set; } = new List<TotalReceipt>();

    public virtual User? User { get; set; }

    public virtual ICollection<VehicleCheckin> VehicleCheckins { get; set; } = new List<VehicleCheckin>();

    public virtual VehicleType? VehicleType { get; set; }
}
