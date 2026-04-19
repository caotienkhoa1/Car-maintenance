using System;
using System.Collections.Generic;

namespace BE.models;

public partial class VehicleCheckin
{
    public long Id { get; set; }

    public long? CarId { get; set; }

    public int? Mileage { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? StatusCode { get; set; }

    public long? BranchId { get; set; }

    public string? Code { get; set; }

    public string? SnapshotCarName { get; set; }

    public string? SnapshotCarModel { get; set; }

    public string? SnapshotVehicleType { get; set; }

    public long? SnapshotVehicleTypeId { get; set; }

    public string? SnapshotLicensePlate { get; set; }

    public string? SnapshotVinNumber { get; set; }

    public string? SnapshotEngineNumber { get; set; }

    public int? SnapshotYearOfManufacture { get; set; }

    public string? SnapshotColor { get; set; }

    public int? SnapshotMileage { get; set; }

    public string? SnapshotCustomerName { get; set; }

    public string? SnapshotCustomerPhone { get; set; }

    public string? SnapshotCustomerEmail { get; set; }

    public string? SnapshotCustomerAddress { get; set; }

    public string? SnapshotBranchName { get; set; }

    public string? SnapshotConsulterName { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual Car? Car { get; set; }

    public virtual ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();

    public virtual StatusLookup? StatusCodeNavigation { get; set; }

    public virtual ICollection<VehicleCheckinImage> VehicleCheckinImages { get; set; } = new List<VehicleCheckinImage>();
}
