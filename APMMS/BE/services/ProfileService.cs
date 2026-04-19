using AutoMapper;
using BE.DTOs.Employee;
using BE.interfaces;
using BE.models;
using BE.repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BE.services
{
    public class ProfileService : IProfileService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly CarMaintenanceDbContext _dbContext;
        private readonly CloudinaryService _cloudinaryService;

        public ProfileService(
            IUserRepository userRepository, 
            IMapper mapper, 
            CarMaintenanceDbContext dbContext,
            CloudinaryService cloudinaryService)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _dbContext = dbContext;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<EmployeeResponseDto?> GetMyProfileAsync(long userId)
        {
            var user = await _userRepository.GetByIdWithIncludesAsync(userId);
            if (user == null)
                return null;

            return _mapper.Map<EmployeeResponseDto>(user);
        }

        public async Task<EmployeeResponseDto> UpdateMyProfileAsync(long userId, EmployeeProfileUpdateDto dto)
        {
            var user = await _userRepository.GetByIdWithIncludesAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("Không tìm thấy người dùng");

            // Không cho phép employee thay đổi roleId, branchId, statusCode, code, password, username của chính mình
            // Chỉ cho phép cập nhật thông tin cá nhân
            // QUAN TRỌNG: KHÔNG BAO GIỜ update password, username, code, roleId, branchId, statusCode khi update profile
            
            if (!string.IsNullOrEmpty(dto.FirstName)) user.FirstName = dto.FirstName;
            if (!string.IsNullOrEmpty(dto.LastName)) user.LastName = dto.LastName;
            if (!string.IsNullOrEmpty(dto.Email)) user.Email = dto.Email;
            if (!string.IsNullOrEmpty(dto.Phone)) user.Phone = dto.Phone;
            if (!string.IsNullOrEmpty(dto.Gender)) user.Gender = dto.Gender;
            if (!string.IsNullOrEmpty(dto.Image)) user.Image = dto.Image;
            if (!string.IsNullOrEmpty(dto.Address)) user.Address = dto.Address;
            if (!string.IsNullOrEmpty(dto.CitizenId)) user.CitizenId = dto.CitizenId;
            if (!string.IsNullOrEmpty(dto.TaxCode)) user.TaxCode = dto.TaxCode;
            
            // Parse Dob từ string format dd-MM-yyyy sang DateOnly
            if (!string.IsNullOrEmpty(dto.Dob))
            {
                if (DateOnly.TryParseExact(dto.Dob, "dd-MM-yyyy", out var dob))
                {
                    user.Dob = dob;
                }
                else if (DateTime.TryParse(dto.Dob, out var dobDateTime))
                {
                    user.Dob = DateOnly.FromDateTime(dobDateTime);
                }
            }
            
            user.LastModifiedDate = DateTime.Now;

            // QUAN TRỌNG: Không dùng Update() vì nó có thể update tất cả properties
            // Entity đã được tracked từ GetByIdWithIncludesAsync, nên chỉ cần SaveChanges
            // Đảm bảo password, username, code, roleId, branchId, statusCode KHÔNG BAO GIỜ bị modified
            _dbContext.Entry(user).Property(u => u.Password).IsModified = false;
            _dbContext.Entry(user).Property(u => u.Username).IsModified = false;
            _dbContext.Entry(user).Property(u => u.Code).IsModified = false;
            _dbContext.Entry(user).Property(u => u.RoleId).IsModified = false;
            _dbContext.Entry(user).Property(u => u.BranchId).IsModified = false;
            _dbContext.Entry(user).Property(u => u.StatusCode).IsModified = false;
            
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<EmployeeResponseDto>(user);
        }

        public async Task<string> UploadAvatarAsync(long userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Không có file được chọn");

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException("Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp)");

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("Kích thước file không được vượt quá 5MB");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("Không tìm thấy người dùng");

            // Upload to Cloudinary (isAvatar = true để crop thành hình vuông)
            var imageUrl = await _cloudinaryService.UploadImageAsync(file, "user-avatars", isAvatar: true);

            // Cập nhật image URL vào database
            user.Image = imageUrl;
            user.LastModifiedDate = DateTime.Now;
            
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();

            return imageUrl;
        }
    }
}


