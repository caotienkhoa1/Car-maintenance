using System;
using System.Collections.Generic;

namespace BE.models;

public partial class StockInRequestDetail
{
    public long StockInRequestId { get; set; }

    public long ComponentId { get; set; }

    public int Quantity { get; set; }

    public virtual Component Component { get; set; } = null!;

    public virtual StockInRequest StockInRequest { get; set; } = null!;
}
