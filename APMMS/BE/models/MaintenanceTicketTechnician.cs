using System;
using System.Collections.Generic;

namespace BE.models;

public partial class MaintenanceTicketTechnician
{
    public long MaintenanceTicketId { get; set; }

    public long TechnicianId { get; set; }

    public DateTime? AssignedDate { get; set; }

    public string? RoleInTicket { get; set; }

    public virtual MaintenanceTicket MaintenanceTicket { get; set; } = null!;

    public virtual User Technician { get; set; } = null!;
}
