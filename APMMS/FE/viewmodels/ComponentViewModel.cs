using System.ComponentModel.DataAnnotations;

namespace FE.viewmodels
{
    public class ComponentViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public decimal? UnitPrice { get; set; }
        public int? QuantityStock { get; set; }
        public string? ImageUrl { get; set; }
        public long? TypeComponentId { get; set; }
        public string? TypeComponentName { get; set; }
        public long? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string Status { get; set; } = "ACTIVE";
    }

    public class CreateComponentViewModel
    {
        [Required(ErrorMessage = "Tên linh kiện là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Mã không được vượt quá 50 ký tự")]
        public string? Code { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public decimal? UnitPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0")]
        public int? QuantityStock { get; set; }

        public long? TypeComponentId { get; set; }

        [Required(ErrorMessage = "Chi nhánh là bắt buộc")]
        public long BranchId { get; set; }

        public string? ImageUrl { get; set; }
    }

    public class UpdateComponentViewModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Tên linh kiện là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Mã không được vượt quá 50 ký tự")]
        public string? Code { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public decimal? UnitPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0")]
        public int? QuantityStock { get; set; }

        public long? TypeComponentId { get; set; }

        [Required(ErrorMessage = "Chi nhánh là bắt buộc")]
        public long BranchId { get; set; }

        public string? ImageUrl { get; set; }
    }

    public class ComponentListViewModel
    {
        public List<ComponentViewModel> Components { get; set; } = new List<ComponentViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public string? SearchTerm { get; set; }
        public long? TypeComponentId { get; set; }
        public string? StatusFilter { get; set; }
    }
}
