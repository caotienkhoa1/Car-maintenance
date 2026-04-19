namespace BE.DTOs.StockInRequest;

public class StockInRequestResponseDto
{
    public long StockInRequestId { get; set; }
    public string Code { get; set; } = string.Empty;
    public long BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? Description { get; set; }
    public string? Note { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string? StatusName { get; set; }
    public long? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? LastModifiedBy { get; set; }
    public string? LastModifiedByName { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    public List<StockInRequestDetailResponseDto>? Details { get; set; }
}

public class StockInRequestDetailResponseDto
{
    public long StockInRequestId { get; set; }
    public long ComponentId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal? ImportPricePerUnit { get; set; }
    public decimal? ExportPricePerUnit { get; set; }
    public int? MinQuantity { get; set; }
}

