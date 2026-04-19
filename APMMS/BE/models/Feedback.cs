using System;
using System.Collections.Generic;

namespace BE.models;

public partial class Feedback
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public long? MaintenanceTicketId { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public long? ParentId { get; set; }

    public virtual ICollection<Feedback> InverseParent { get; set; } = new List<Feedback>();

    public virtual MaintenanceTicket? MaintenanceTicket { get; set; }

    public virtual Feedback? Parent { get; set; }

    public virtual User? User { get; set; }
}
