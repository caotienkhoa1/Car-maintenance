using System;
using System.Collections.Generic;

namespace BE.models;

public partial class HistoryLog
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public string? Action { get; set; }

    public string? OldData { get; set; }

    public string? NewData { get; set; }

    public DateTime? CreatedAt { get; set; }

    public long? MaintenanceTicketId { get; set; }

    public virtual MaintenanceTicket? MaintenanceTicket { get; set; }

    public virtual User? User { get; set; }
}
