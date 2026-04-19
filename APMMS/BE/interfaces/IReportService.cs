namespace BE.interfaces
{
    public interface IReportService
    {
        Task<byte[]> GenerateMaintenanceTicketPdfAsync(long maintenanceTicketId);
        Task<byte[]> GenerateTotalReceiptPdfAsync(long totalReceiptId);
        Task<byte[]> GenerateQuotationPdfAsync(long maintenanceTicketId);
        Task<byte[]> GenerateProvisionalInvoicePdfAsync(long maintenanceTicketId);
    }
}


