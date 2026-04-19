using System.ComponentModel.DataAnnotations;

namespace FE.viewmodels
{
    public class MaintenanceViewModel
    {
        public long Id { get; set; }
        
        [Required(ErrorMessage = "Mã phiếu bảo dưỡng là bắt buộc")]
        [Display(Name = "Mã phiếu")]
        public string Code { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Mã xe là bắt buộc")]
        [Display(Name = "Mã xe")]
        public string VehicleCode { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Loại bảo dưỡng là bắt buộc")]
        [Display(Name = "Loại bảo dưỡng")]
        public string MaintenanceType { get; set; } = string.Empty;
        
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; }
        
        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.DateTime)]
        public DateTime? EndDate { get; set; }
        
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Pending";
        
        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }
        
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }

    public class MaintenanceListViewModel
    {
        public List<MaintenanceViewModel> Maintenances { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
    }
}


