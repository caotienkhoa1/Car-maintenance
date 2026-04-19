using System.Collections.Generic;

namespace BE.DTOs.StockInRequest
{
    public class StockInRequestUploadResponseDto
    {
        public List<StockInRequestDetailUploadDto> Details { get; set; } = new List<StockInRequestDetailUploadDto>();
    }

    public class StockInRequestDetailUploadDto
    {
        public long ComponentId { get; set; }
        public string ComponentCode { get; set; } = "";
        public string ComponentName { get; set; } = "";
        public string? TypeComponentName { get; set; }
        public int Quantity { get; set; }
    }
}
