using System.ComponentModel.DataAnnotations;

namespace FE.viewmodels
{
    public class PartsViewModel
    {
        public long Id { get; set; }
        
        [Required(ErrorMessage = "Mã phụ tùng là bắt buộc")]
        [Display(Name = "Mã phụ tùng")]
        public string Code { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Tên phụ tùng là bắt buộc")]
        [Display(Name = "Tên phụ tùng")]
        public string Name { get; set; } = string.Empty;
        
        [Display(Name = "Loại phụ tùng")]
        public string? Type { get; set; }
        
        [Display(Name = "Nhà sản xuất")]
        public string? Manufacturer { get; set; }
        
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Display(Name = "Số lượng")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0")]
        public int Quantity { get; set; }
        
        [Display(Name = "Đơn giá")]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0")]
        public decimal? UnitPrice { get; set; }
        
        [Display(Name = "Đơn vị")]
        public string? Unit { get; set; }
        
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Available";
        
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }

    public class PartsListViewModel
    {
        public List<PartsViewModel> Parts { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? TypeFilter { get; set; }
    }
}


