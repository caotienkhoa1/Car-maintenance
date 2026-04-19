using System;

namespace BE.DTOs.TotalReceipt
{
    public class RequestDto
    {
        public long? Id { get; set; }
        public long? MaintenanceTicketId { get; set; }
        public long? CarId { get; set; }
        public long? BranchId { get; set; }
        public long? AccountantId { get; set; }
        public decimal? Amount { get; set; }
        public string? CurrencyCode { get; set; }
        public string? StatusCode { get; set; }
        public string? Note { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? VatPercent { get; set; }
        public decimal? VatAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? SurchargeAmount { get; set; }
        public decimal? FinalAmount { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}


