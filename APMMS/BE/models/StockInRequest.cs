using System;
using System.Collections.Generic;

namespace BE.models;

public partial class StockInRequest
{
    public long Id { get; set; }

    public string Code { get; set; } = null!;

    public long BranchId { get; set; }

    public string? Description { get; set; }

    public string? Note { get; set; }

    public string StatusCode { get; set; } = null!;

    public long? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual User? CreatedByNavigation { get; set; }

    public virtual User? LastModifiedByNavigation { get; set; }

    public virtual StatusLookup StatusCodeNavigation { get; set; } = null!;

    public virtual StockIn? StockIn { get; set; }

    public virtual ICollection<StockInRequestDetail> StockInRequestDetails { get; set; } = new List<StockInRequestDetail>();
}
