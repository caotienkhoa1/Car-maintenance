using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.Branch
{
    public class BranchRequestDto
    {
        [Required(ErrorMessage = "Tên chi nhánh là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Tên chi nhánh không được vượt quá 255 ký tự")]
        public string Name { get; set; } = null!;

        [RegularExpression(@"^(0[35789])+([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ. Phải bắt đầu bằng 0 và có 10 chữ số")]
        [MaxLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string? Phone { get; set; }

        [MaxLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        public string? Address { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá công lao động phải lớn hơn hoặc bằng 0")]
        public decimal? LaborRate { get; set; }
    }
}

