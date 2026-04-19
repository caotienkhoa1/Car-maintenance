using System;
using System.Collections.Generic;

namespace BE.models;

public partial class StockInDetail
{
    public long Id { get; set; }

    public long StockInId { get; set; }

    public long ComponentId { get; set; }

    public int Quantity { get; set; }

    public int? QuantityAfterCheck { get; set; }

    public decimal? ImportPricePerUnit { get; set; }

    public decimal? ExportPricePerUnit { get; set; }

    public decimal? Vat { get; set; }

    public int? MinQuantity { get; set; }

    public string? ComponentCode { get; set; }

    public string? ComponentName { get; set; }

    public string? TypeComponent { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public virtual Component Component { get; set; } = null!;

    public virtual StockIn StockIn { get; set; } = null!;
}
