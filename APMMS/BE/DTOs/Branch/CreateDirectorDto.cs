using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.Branch
{
    /// <summary>
    /// DTO để tạo giám đốc chi nhánh mới (độc lập với Employee)
    /// </summary>
    public class CreateDirectorDto
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [MinLength(6, ErrorMessage = "Tên đăng nhập phải có ít nhất 6 ký tự")]
        [MaxLength(12, ErrorMessage = "Tên đăng nhập không được vượt quá 12 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [MaxLength(12, ErrorMessage = "Mật khẩu không được vượt quá 12 ký tự")]
        public string Password { get; set; } = null!;

        [MaxLength(100, ErrorMessage = "Họ không được vượt quá 100 ký tự")]
        public string? FirstName { get; set; }

        [MaxLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        public string? LastName { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string? Email { get; set; }

        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ. Phải bắt đầu bằng 0 và có 10 chữ số")]
        [MaxLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string? Phone { get; set; }

        [MaxLength(20, ErrorMessage = "Giới tính không được vượt quá 20 ký tự")]
        public string? Gender { get; set; }

        [MaxLength(255, ErrorMessage = "URL ảnh không được vượt quá 255 ký tự")]
        public string? Image { get; set; }

        public string? Dob { get; set; } // Format: dd-MM-yyyy

        [RegularExpression(@"^[0-9]{12}$", ErrorMessage = "CMT/CCCD phải có đúng 12 chữ số")]
        [MaxLength(12, ErrorMessage = "CMT/CCCD không được vượt quá 12 ký tự")]
        public string? CitizenId { get; set; }

        [MaxLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
        public string? Address { get; set; }

        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mã số thuế phải có đúng 10 chữ số")]
        [MaxLength(10, ErrorMessage = "Mã số thuế không được vượt quá 10 ký tự")]
        public string? TaxCode { get; set; }
    }
}

