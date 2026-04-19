using AutoMapper;
using BE.DTOs.Branch;
using BE.DTOs.Employee;
using BE.interfaces;
using BE.models;
using BE.repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace BE.services
{
    public class BranchService : IBranchService
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IMapper _mapper;
        private readonly CarMaintenanceDbContext _dbContext;
        private const long DIRECTOR_ROLE_ID = 2; // Giả sử RoleId = 2 là Giám đốc chi nhánh

        public BranchService(IBranchRepository branchRepository, IMapper mapper, CarMaintenanceDbContext dbContext)
        {
            _branchRepository = branchRepository;
            _mapper = mapper;
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<BranchResponseDto>> GetAllAsync()
        {
            var branches = await _branchRepository.GetAllAsync();

            // Map kèm thông tin giám đốc cho từng chi nhánh để hiển thị ở danh sách
            var result = new List<BranchResponseDto>();
            foreach (var branch in branches)
            {
                var dto = _mapper.Map<BranchResponseDto>(branch);
                dto.Director = await GetDirectorAsync(branch.Id);
                result.Add(dto);
            }

            return result;
        }

        public async Task<BranchResponseDto?> GetByIdAsync(long id)
        {
            var branch = await _branchRepository.GetByIdAsync(id);
            if (branch == null) return null;
            
            var branchDto = _mapper.Map<BranchResponseDto>(branch);
            
            // Load director information
            var director = await GetDirectorAsync(id);
            branchDto.Director = director;
            
            return branchDto;
        }

        public async Task<BranchResponseDto> CreateAsync(BranchRequestDto dto)
        {
            var branch = _mapper.Map<Branch>(dto);
            await _branchRepository.AddAsync(branch);
            await _branchRepository.SaveChangesAsync();
            return _mapper.Map<BranchResponseDto>(branch);
        }

        public async Task<BranchResponseDto?> UpdateAsync(long id, BranchRequestDto dto)
        {
            var branch = await _branchRepository.GetByIdAsync(id);
            if (branch == null) return null;

            branch.Name = dto.Name;
            branch.Phone = dto.Phone;
            branch.Address = dto.Address;
            branch.LaborRate = dto.LaborRate;

            await _branchRepository.UpdateAsync(branch);
            await _branchRepository.SaveChangesAsync();

            return _mapper.Map<BranchResponseDto>(branch);
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var branch = await _branchRepository.GetByIdAsync(id);
            if (branch == null) return false;

            // Kiểm tra xem chi nhánh có đang được sử dụng không
            var isInUse = await _branchRepository.IsBranchInUseAsync(id);
            if (isInUse)
            {
                throw new InvalidOperationException("Không thể xóa chi nhánh này vì đang được sử dụng trong hệ thống");
            }

            await _branchRepository.DeleteAsync(branch);
            await _branchRepository.SaveChangesAsync();
            return true;
        }

        public async Task<EmployeeResponseDto?> GetDirectorAsync(long branchId)
        {
            // Tìm giám đốc: User có BranchId = branchId, RoleId = 2 (Giám đốc), StatusCode = "ACTIVE"
            var director = await _dbContext.Users
                .Include(u => u.Role)
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => 
                    u.BranchId == branchId && 
                    u.RoleId == DIRECTOR_ROLE_ID && 
                    u.StatusCode == "ACTIVE" &&
                    (u.IsDelete == null || u.IsDelete == false));

            if (director == null) return null;

            return _mapper.Map<EmployeeResponseDto>(director);
        }

        public async Task<bool> ChangeDirectorAsync(long branchId, long newDirectorId)
        {
            // Kiểm tra branch có tồn tại không
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                throw new KeyNotFoundException("Không tìm thấy chi nhánh");
            }

            // Kiểm tra nhân viên mới có tồn tại không
            var newDirector = await _dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == newDirectorId);
            
            if (newDirector == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhân viên");
            }

            // ✅ Tìm giám đốc cũ của chi nhánh (trừ chính nhân viên mới nếu đã là giám đốc)
            var oldDirector = await _dbContext.Users
                .FirstOrDefaultAsync(u => 
                    u.BranchId == branchId && 
                    u.RoleId == DIRECTOR_ROLE_ID && 
                    u.StatusCode == "ACTIVE" &&
                    (u.IsDelete == null || u.IsDelete == false) &&
                    u.Id != newDirectorId); // ✅ Tránh set chính nhân viên mới thành INACTIVE

            // ✅ Set giám đốc cũ thành INACTIVE (nếu có và không phải chính nhân viên mới)
            if (oldDirector != null)
            {
                oldDirector.StatusCode = "INACTIVE";
                oldDirector.LastModifiedDate = DateTime.Now;
            }

            // ✅ Set nhân viên mới thành giám đốc
            // Cho phép gán nhân viên từ chi nhánh khác - tự động set BranchId
            newDirector.RoleId = DIRECTOR_ROLE_ID;
            newDirector.BranchId = branchId; // ✅ Đảm bảo BranchId đúng
            newDirector.StatusCode = "ACTIVE";
            newDirector.LastModifiedDate = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// ✅ Tạo giám đốc chi nhánh mới (độc lập với Employee)
        /// </summary>
        public async Task<EmployeeResponseDto> CreateDirectorAsync(long branchId, CreateDirectorDto dto, long? createdByUserId = null)
        {
            // Kiểm tra branch có tồn tại không
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                throw new KeyNotFoundException("Không tìm thấy chi nhánh");
            }

            // Kiểm tra username đã tồn tại chưa
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (existingUser != null)
            {
                throw new ArgumentException($"Tên đăng nhập '{dto.Username}' đã tồn tại. Vui lòng chọn tên khác.");
            }

            // ✅ Kiểm tra chi nhánh đã có giám đốc chưa - nếu có thì tự động thay thế
            var existingDirector = await _dbContext.Users
                .FirstOrDefaultAsync(u => 
                    u.BranchId == branchId && 
                    u.RoleId == DIRECTOR_ROLE_ID && 
                    u.StatusCode == "ACTIVE" &&
                    (u.IsDelete == null || u.IsDelete == false));
            
            if (existingDirector != null)
            {
                // ✅ Tự động chuyển giám đốc cũ sang role Consultant (RoleId = 6)
                existingDirector.RoleId = 6; // Consultant role
                existingDirector.LastModifiedDate = DateTime.Now;
            }

            // ✅ Tạo user mới với role Branch Manager
            var newDirector = new User
            {
                Username = dto.Username,
                Password = HashPassword(dto.Password),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                Gender = dto.Gender,
                Image = dto.Image,
                CitizenId = dto.CitizenId,
                TaxCode = dto.TaxCode,
                Address = dto.Address,
                RoleId = DIRECTOR_ROLE_ID, // Branch Manager
                BranchId = branchId,
                StatusCode = "ACTIVE",
                CreatedDate = DateTime.Now,
                IsDelete = false,
                CreatedBy = createdByUserId
            };

            // ✅ Parse ngày sinh nếu có
            if (!string.IsNullOrWhiteSpace(dto.Dob))
            {
                if (DateOnly.TryParseExact(dto.Dob, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out var dob))
                {
                    newDirector.Dob = dob;
                }
            }

            // ✅ Sinh mã tự động cho giám đốc (prefix: BM)
            newDirector.Code = await GenerateUniqueDirectorCodeAsync();

            // ✅ Thêm vào database
            await _dbContext.Users.AddAsync(newDirector);
            await _dbContext.SaveChangesAsync();

            // ✅ Load lại với navigation properties
            var createdDirector = await _dbContext.Users
                .Include(u => u.Role)
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Id == newDirector.Id);

            return _mapper.Map<EmployeeResponseDto>(createdDirector);
        }

        private static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return password;
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        /// <summary>
        /// Sinh mã duy nhất cho giám đốc: BM + 5 số ngẫu nhiên (vd: BM04218)
        /// </summary>
        private async Task<string> GenerateUniqueDirectorCodeAsync()
        {
            const string prefix = "BM";
            const int digits = 5;
            var rnd = new Random();

            for (int i = 0; i < 50; i++)
            {
                var number = rnd.Next(0, (int)Math.Pow(10, digits)).ToString($"D{digits}");
                var code = $"{prefix}{number}";

                var exists = await _dbContext.Users.AsNoTracking().AnyAsync(u => u.Code == code);
                if (!exists) return code;
            }

            // Fallback (hầu như không bao giờ xảy ra)
            return $"{prefix}{DateTime.UtcNow:HHmmssff}";
        }
    }
}

