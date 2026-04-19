using System;
using System.Collections.Generic;

namespace BE.models;

public partial class Component
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public decimal? UnitPrice { get; set; }

    public int? QuantityStock { get; set; }

    public long? TypeComponentId { get; set; }

    public long? BranchId { get; set; }

    public string? ImageUrl { get; set; }

    public string? StatusCode { get; set; }

    public decimal? PurchasePrice { get; set; }

    public int? MinimumQuantity { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual ICollection<ComponentPackage> ComponentPackages { get; set; } = new List<ComponentPackage>();

    public virtual StatusLookup? StatusCodeNavigation { get; set; }

    public virtual ICollection<StockInDetail> StockInDetails { get; set; } = new List<StockInDetail>();

    public virtual ICollection<StockInRequestDetail> StockInRequestDetails { get; set; } = new List<StockInRequestDetail>();

    public virtual ICollection<TicketComponent> TicketComponents { get; set; } = new List<TicketComponent>();

    public virtual TypeComponent? TypeComponent { get; set; }
}
