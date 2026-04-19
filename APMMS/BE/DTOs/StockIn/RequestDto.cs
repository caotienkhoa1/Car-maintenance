using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.StockIn;

public class StockInRequestDto
{
    public long? Id { get; set; }
    
    [Required(ErrorMessage = "Mã yêu cầu nhập kho là bắt buộc")]
    public string StockInRequestCode { get; set; } = string.Empty;
    
    public long? StockInRequestId { get; set; }
    
    public string StatusCode { get; set; } = "PENDING";
    
    public List<StockInDetailDto>? Details { get; set; }
}

public class StockInDetailDto
{
    public long? Id { get; set; }
    
    [Required(ErrorMessage = "Linh kiện là bắt buộc")]
    public long? ComponentId { get; set; }
    
    public string ComponentCode { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Tên linh kiện là bắt buộc")]
    public string ComponentName { get; set; } = string.Empty;
    
    public string? TypeComponent { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public int Quantity { get; set; }
    
    [Range(0, int.MaxValue, ErrorMessage = "Số lượng sau kiểm tra phải >= 0")]
    public int? QuantityAfterCheck { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Giá nhập phải >= 0")]
    public decimal? ImportPricePerUnit { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Giá xuất phải >= 0")]
    public decimal? ExportPricePerUnit { get; set; }
    
    [Range(0, 100, ErrorMessage = "VAT phải từ 0 đến 100")]
    public decimal? Vat { get; set; }
    
    public int? MinQuantity { get; set; }
}

