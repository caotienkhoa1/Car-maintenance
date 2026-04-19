using System;
using System.Collections.Generic;

namespace BE.models;

public partial class ScheduleServiceNote
{
    public long Id { get; set; }

    public long ScheduleServiceId { get; set; }

    public long ConsultantId { get; set; }

    public string Note { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User Consultant { get; set; } = null!;

    public virtual ScheduleService ScheduleService { get; set; } = null!;
}
