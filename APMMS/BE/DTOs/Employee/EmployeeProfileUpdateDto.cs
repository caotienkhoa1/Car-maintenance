using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.Employee
{
    /// <summary>
    /// DTO cho việc cập nhật profile của chính employee
    /// Không có Username và Password vì không được phép thay đổi
    /// </summary>
    public class EmployeeProfileUpdateDto
    {
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

