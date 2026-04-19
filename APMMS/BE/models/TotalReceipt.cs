using System;
using System.Collections.Generic;

namespace BE.models;

public partial class TotalReceipt
{
    public long Id { get; set; }

    public long? CarId { get; set; }

    public long? MaintenanceTicketId { get; set; }

    public decimal Amount { get; set; }

    public string? CurrencyCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public long? AccountantId { get; set; }

    public long? BranchId { get; set; }

    public string? StatusCode { get; set; }

    public string? Note { get; set; }

    public decimal? Subtotal { get; set; }

    public decimal? VatPercent { get; set; }

    public decimal? VatAmount { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? SurchargeAmount { get; set; }

    public decimal? FinalAmount { get; set; }

    public long? ServicePackageId { get; set; }

    public decimal? ServicePackagePrice { get; set; }

    public string? ServicePackageName { get; set; }

    public decimal? PackageDiscountAmount { get; set; }

    public virtual User? Accountant { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual Car? Car { get; set; }

    public virtual MaintenanceTicket? MaintenanceTicket { get; set; }

    public virtual ServicePackage? ServicePackage { get; set; }

    public virtual StatusLookup? StatusCodeNavigation { get; set; }
}
