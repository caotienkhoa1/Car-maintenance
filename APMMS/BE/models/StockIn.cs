using System;
using System.Collections.Generic;

namespace BE.models;

public partial class StockIn
{
    public long Id { get; set; }

    public long StockInRequestId { get; set; }

    public string StatusCode { get; set; } = null!;

    public long? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual User? LastModifiedByNavigation { get; set; }

    public virtual StatusLookup StatusCodeNavigation { get; set; } = null!;

    public virtual ICollection<StockInDetail> StockInDetails { get; set; } = new List<StockInDetail>();

    public virtual StockInRequest StockInRequest { get; set; } = null!;
}
