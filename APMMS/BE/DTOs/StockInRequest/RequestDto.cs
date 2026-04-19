using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.StockInRequest;

public class StockInRequestRequestDto
{
    public long? StockInRequestId { get; set; }
    
    // Code sẽ được tự động tạo bởi backend, không cần Required
    public string Code { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Chi nhánh là bắt buộc")]
    public long BranchId { get; set; }
    
    public string? Description { get; set; }
    
    public string? Note { get; set; }
    
    public string StatusCode { get; set; } = "PENDING";
    
    public List<StockInRequestDetailDto>? Details { get; set; }
}

public class StockInRequestDetailDto
{
    [Required(ErrorMessage = "Linh kiện là bắt buộc")]
    public long ComponentId { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public int Quantity { get; set; }
    
    // Thông tin hiển thị (không bắt buộc khi tạo)
    public string? ComponentName { get; set; }
    public string? ComponentCode { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? ImportPricePerUnit { get; set; }
    public decimal? ExportPricePerUnit { get; set; }
    public int? MinQuantity { get; set; }
}

