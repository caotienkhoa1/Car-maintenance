using System.ComponentModel.DataAnnotations;

namespace FE.viewmodels
{
    public class VehicleViewModel
    {
        public long Id { get; set; }
        
        [Required(ErrorMessage = "Mã xe là bắt buộc")]
        [Display(Name = "Mã xe")]
        public string Code { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Tên xe là bắt buộc")]
        [Display(Name = "Tên xe")]
        public string Name { get; set; } = string.Empty;
        
        [Display(Name = "Loại xe")]
        public string? Type { get; set; }
        
        [Display(Name = "Năm sản xuất")]
        public int? Year { get; set; }
        
        [Display(Name = "Màu sắc")]
        public string? Color { get; set; }
        
        [Display(Name = "Số khung")]
        public string? ChassisNumber { get; set; }
        
        [Display(Name = "Số máy")]
        public string? EngineNumber { get; set; }
        
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Active";
        
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }

    public class VehicleListViewModel
    {
        public List<VehicleViewModel> Vehicles { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
    }
}


