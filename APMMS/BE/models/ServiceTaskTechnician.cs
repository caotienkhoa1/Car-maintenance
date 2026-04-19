using System;
using System.Collections.Generic;

namespace BE.models;

public partial class ServiceTaskTechnician
{
    public long ServiceTaskId { get; set; }

    public long TechnicianId { get; set; }

    public DateTime? AssignedDate { get; set; }

    public string? RoleInTask { get; set; }

    public virtual ServiceTask ServiceTask { get; set; } = null!;

    public virtual User Technician { get; set; } = null!;
}
