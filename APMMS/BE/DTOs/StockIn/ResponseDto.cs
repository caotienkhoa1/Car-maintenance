namespace BE.DTOs.StockIn;

public class StockInResponseDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty; // Mã phiếu nhập kho
    public long StockInRequestId { get; set; }
    public string StockInRequestCode { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string? StatusName { get; set; }
    public long? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public long? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? LastModifiedBy { get; set; }
    public string? LastModifiedByName { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    public List<StockInDetailResponseDto>? Details { get; set; }
}

public class StockInDetailResponseDto
{
    public long Id { get; set; }
    public long StockInId { get; set; }
    public long ComponentId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string? TypeComponent { get; set; }
    public int Quantity { get; set; }
    public int? QuantityAfterCheck { get; set; }
    public decimal? ImportPricePerUnit { get; set; }
    public decimal? ExportPricePerUnit { get; set; }
    public decimal? Vat { get; set; }
    public int? MinQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedDate { get; set; }
}

