using System.ComponentModel.DataAnnotations;

namespace FE.viewmodels
{
    public class UserViewModel
    {
        public long Id { get; set; }
        
        [Required(ErrorMessage = "Mã người dùng là bắt buộc")]
        [Display(Name = "Mã người dùng")]
        public string Code { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;
        
        [Display(Name = "Họ tên")]
        public string? FullName { get; set; }
        
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }
        
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }
        
        [Display(Name = "Giới tính")]
        public string? Gender { get; set; }
        
        [Display(Name = "Vai trò")]
        public string? Role { get; set; }
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Chi nhánh")]
        public string? BranchName { get; set; }
        [Display(Name = "Căn cước công dân")]
        public string? CitizenId { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Active";
        public DateOnly? Dob { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }

    public class UserListViewModel
    {
        public List<UserViewModel> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? RoleFilter { get; set; }
    }
}


