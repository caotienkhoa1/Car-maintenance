using System.Collections.Generic;

namespace BE.DTOs.StockIn
{
    public class StockInUploadResponseDto
    {
        public string? StockInRequestCode { get; set; }
        public List<StockInDetailUploadDto> Details { get; set; } = new List<StockInDetailUploadDto>();
    }

    public class StockInDetailUploadDto
    {
        public long ComponentId { get; set; }
        public string? ComponentCode { get; set; }
        public string? ComponentName { get; set; }
        public string? TypeComponentName { get; set; }
        public int Quantity { get; set; }
        public int QuantityAfterCheck { get; set; }
        public decimal? ImportPricePerUnit { get; set; }
        public decimal? ExportPricePerUnit { get; set; }
    }
}

