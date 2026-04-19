using System;
using System.Collections.Generic;

namespace BE.models;

public partial class MaintenanceTicket
{
    public long Id { get; set; }

    public long? ScheduleServiceId { get; set; }

    public long? CarId { get; set; }

    public long? ConsulterId { get; set; }

    public long? TechnicianId { get; set; }

    public string? StatusCode { get; set; }

    public long? BranchId { get; set; }

    public string? Description { get; set; }

    public long? VehicleCheckinId { get; set; }

    public string? Code { get; set; }

    public decimal? TotalEstimatedCost { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? PriorityLevel { get; set; }

    public DateTime? CreatedAt { get; set; }

    public long? ServiceCategoryId { get; set; }

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

    public long? ServicePackageId { get; set; }

    public decimal? ServicePackagePrice { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual Car? Car { get; set; }

    public virtual User? Consulter { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<HistoryLog> HistoryLogs { get; set; } = new List<HistoryLog>();

    public virtual ICollection<MaintenanceTicketTechnician> MaintenanceTicketTechnicians { get; set; } = new List<MaintenanceTicketTechnician>();

    public virtual ScheduleService? ScheduleService { get; set; }

    public virtual ServiceCategory? ServiceCategory { get; set; }

    public virtual ServicePackage? ServicePackage { get; set; }

    public virtual ICollection<ServiceTask> ServiceTasks { get; set; } = new List<ServiceTask>();

    public virtual StatusLookup? StatusCodeNavigation { get; set; }

    public virtual User? Technician { get; set; }

    public virtual ICollection<TicketComponent> TicketComponents { get; set; } = new List<TicketComponent>();

    public virtual ICollection<TotalReceipt> TotalReceipts { get; set; } = new List<TotalReceipt>();

    public virtual VehicleCheckin? VehicleCheckin { get; set; }
}
