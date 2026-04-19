using System;
using System.Collections.Generic;

namespace BE.models;

public partial class ScheduleService
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public long? CarId { get; set; }

    public DateTime ScheduledDate { get; set; }

    public string? StatusCode { get; set; }

    public long? BranchId { get; set; }

    public long? ServiceCategoryId { get; set; }

    public long? GuestId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? CreatedBy { get; set; }

    public long? UpdatedBy { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual Car? Car { get; set; }

    public virtual CustomerGuest? Guest { get; set; }

    public virtual ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();

    public virtual ICollection<ScheduleServiceNote> ScheduleServiceNotes { get; set; } = new List<ScheduleServiceNote>();

    public virtual ServiceCategory? ServiceCategory { get; set; }

    public virtual StatusLookup? StatusCodeNavigation { get; set; }

    public virtual User? User { get; set; }
}
