using AutoMapper;
using BE.DTOs.AutoOwner;
using BE.models;
using BE.repository.IRepository;
using BE.interfaces;
using Microsoft.EntityFrameworkCore;

namespace BE.services
{
    public class AutoOwnerService : IAutoOwnerService
    {
        private readonly IAutoOwnerRepository _repository;
        private readonly IMapper _mapper;
        private readonly CarMaintenanceDbContext _db;

        public AutoOwnerService(IAutoOwnerRepository repository, IMapper mapper, CarMaintenanceDbContext db)
        {
            _repository = repository;
            _mapper = mapper;
            _db = db;
        }

        public async Task<List<ResponseDto>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var users = await _repository.GetAllAsync(page, pageSize);
            return _mapper.Map<List<ResponseDto>>(users);
        }

        public async Task<object> GetWithFiltersAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null, long? roleId = null)
        {
            var users = await _repository.GetWithFiltersAsync(page, pageSize, search, status, roleId);
            var totalCount = await _repository.GetTotalCountAsync(search, status, roleId);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            
            return new
            {
                success = true,
                data = _mapper.Map<List<ResponseDto>>(users),
                page = page,
                pageSize = pageSize,
                totalPages = totalPages,
                currentPage = page,
                totalCount = totalCount
            };
        }

        public async Task<ResponseDto?> GetByIdAsync(long id)
        {
            var user = await _repository.GetByIdAsync(id);
            return _mapper.Map<ResponseDto?>(user);
        }

        public async Task<ResponseDto> CreateAsync(RequestDto dto)
        {
            // Basic field normalization
            dto.Username = dto.Username?.Trim() ?? string.Empty;
            dto.Email = dto.Email?.Trim();
            // ✅ Validate required fields
            if (string.IsNullOrWhiteSpace(dto.Username))
            {
                throw new ArgumentException("Tên đăng nhập là bắt buộc.");
            }
            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            {
                throw new ArgumentException("Họ và Tên là bắt buộc.");
            }
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new ArgumentException("Email là bắt buộc.");
            }

            // ✅ Validate email format (basic)
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                try
                {
                    var _ = new System.Net.Mail.MailAddress(dto.Email);
                }
                catch
                {
                    throw new ArgumentException("Định dạng email không hợp lệ.");
                }
            }

            dto.Phone = dto.Phone?.Trim();
            dto.CitizenId = dto.CitizenId?.Trim();
            dto.TaxCode = dto.TaxCode?.Trim();

            // ✅ Validate phone: must be 10 digits and start with 0
            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Phone, @"^0\d{9}$"))
                {
                    throw new ArgumentException("Số điện thoại không hợp lệ. Vui lòng nhập đúng 10 số và bắt đầu bằng số 0.");
                }
            }

            // ✅ Validate DOB: must be at least 18 years old
            // ✅ Validate CitizenId format: exactly 12 digits if provided
            if (!string.IsNullOrWhiteSpace(dto.CitizenId))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(dto.CitizenId, @"^\d{12}$"))
                {
                    throw new ArgumentException("CCCD không hợp lệ. Vui lòng nhập đúng 12 chữ số.");
                }
            }

            // ✅ Validate TaxCode format if provided: 10 digits OR 10 digits-3 digits
            if (!string.IsNullOrWhiteSpace(dto.TaxCode))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(dto.TaxCode, @"^([0-9]{10}|[0-9]{10}-[0-9]{3})$"))
                {
                    throw new ArgumentException("Mã số thuế không hợp lệ. Định dạng hợp lệ: 10 số hoặc 10 số-3 số (VD: 0123456789 hoặc 0123456789-001).");
                }
            }
            if (dto.Dob.HasValue)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var age = today.Year - dto.Dob.Value.Year - (today.DayOfYear < dto.Dob.Value.DayOfYear ? 1 : 0);
                if (age < 18)
                {
                    throw new ArgumentException("Khách hàng phải từ 18 tuổi trở lên.");
                }
            }

            // ✅ Check duplicates: Username, Email, Phone, CitizenId
            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                var existingUsername = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == dto.Username && (u.IsDelete == false || u.IsDelete == null));
                if (existingUsername != null)
                {
                    throw new ArgumentException("Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var existingEmail = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == dto.Email && (u.IsDelete == false || u.IsDelete == null));
                if (existingEmail != null)
                {
                    throw new ArgumentException("Email đã tồn tại. Vui lòng sử dụng email khác.");
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                var existingPhone = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Phone == dto.Phone && (u.IsDelete == false || u.IsDelete == null));
                if (existingPhone != null)
                {
                    throw new ArgumentException("Số điện thoại đã tồn tại. Vui lòng sử dụng số khác.");
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.CitizenId))
            {
                var existingCitizen = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.CitizenId == dto.CitizenId && (u.IsDelete == false || u.IsDelete == null));
                if (existingCitizen != null)
                {
                    throw new ArgumentException("CCCD đã tồn tại. Vui lòng kiểm tra lại.");
                }
            }

            var user = _mapper.Map<User>(dto);

            // Business rules
            user.RoleId = 7; // AutoOwner role
            user.StatusCode = "ACTIVE";
            user.CreatedDate = DateTime.UtcNow;
            user.IsDelete = false;

            // ✅ Hash password when creating (avoid storing plain text)
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dto.Password));
                    user.Password = Convert.ToBase64String(hashedBytes);
                }
            }
            
            // Explicitly set Address to ensure it's saved
            user.Address = dto.Address;

            // Tự động sinh mã người dùng: KH + 5 số (KH00001, KH00002, ...)
            if (string.IsNullOrWhiteSpace(user.Code))
            {
                user.Code = await GenerateUniqueAutoOwnerCodeAsync();
            }

            await _repository.CreateAsync(user);
            return _mapper.Map<ResponseDto>(user);
        }

        public async Task<ResponseDto> UpdateAsync(long id, RequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException("Auto Owner not found.");

            // Chuẩn hóa dữ liệu đầu vào
            dto.Username = dto.Username?.Trim() ?? existing.Username;
            dto.Email = dto.Email?.Trim() ?? existing.Email;
            dto.Phone = dto.Phone?.Trim() ?? existing.Phone;
            dto.CitizenId = dto.CitizenId?.Trim() ?? existing.CitizenId;

            // ✅ Validate required fields (không cho phép clear Họ/Tên/Email)
            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            {
                throw new ArgumentException("Họ và Tên là bắt buộc.");
            }
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new ArgumentException("Email là bắt buộc.");
            }

            // ✅ Validate email format
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                try
                {
                    var _ = new System.Net.Mail.MailAddress(dto.Email);
                }
                catch
                {
                    throw new ArgumentException("Định dạng email không hợp lệ.");
                }
            }

            // ✅ Validate phone nếu có truyền
            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Phone, @"^0\d{9}$"))
                {
                    throw new ArgumentException("Số điện thoại không hợp lệ. Vui lòng nhập đúng 10 số và bắt đầu bằng số 0.");
                }
            }

            // ✅ Validate DOB >= 18 tuổi nếu có truyền
            // ✅ Validate CitizenId format nếu có truyền
            if (!string.IsNullOrWhiteSpace(dto.CitizenId))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(dto.CitizenId, @"^\d{12}$"))
                {
                    throw new ArgumentException("CCCD không hợp lệ. Vui lòng nhập đúng 12 chữ số.");
                }
            }

            // ✅ Validate TaxCode format nếu có truyền
            if (!string.IsNullOrWhiteSpace(dto.TaxCode))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(dto.TaxCode, @"^([0-9]{10}|[0-9]{10}-[0-9]{3})$"))
                {
                    throw new ArgumentException("Mã số thuế không hợp lệ. Định dạng hợp lệ: 10 số hoặc 10 số-3 số (VD: 0123456789 hoặc 0123456789-001).");
                }
            }
            if (dto.Dob.HasValue)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var age = today.Year - dto.Dob.Value.Year - (today.DayOfYear < dto.Dob.Value.DayOfYear ? 1 : 0);
                if (age < 18)
                {
                    throw new ArgumentException("Khách hàng phải từ 18 tuổi trở lên.");
                }
            }

            // ✅ Check duplicates khi update (bỏ qua chính user hiện tại)
            if (!string.IsNullOrWhiteSpace(dto.Username) && !string.Equals(dto.Username, existing.Username, StringComparison.OrdinalIgnoreCase))
            {
                var existingUsername = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == dto.Username && u.Id != id && (u.IsDelete == false || u.IsDelete == null));
                if (existingUsername != null)
                {
                    throw new ArgumentException("Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.Email) && !string.Equals(dto.Email, existing.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existingEmail = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Id != id && (u.IsDelete == false || u.IsDelete == null));
                if (existingEmail != null)
                {
                    throw new ArgumentException("Email đã tồn tại. Vui lòng sử dụng email khác.");
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.Phone) && !string.Equals(dto.Phone, existing.Phone, StringComparison.OrdinalIgnoreCase))
            {
                var existingPhone = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Phone == dto.Phone && u.Id != id && (u.IsDelete == false || u.IsDelete == null));
                if (existingPhone != null)
                {
                    throw new ArgumentException("Số điện thoại đã tồn tại. Vui lòng sử dụng số khác.");
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.CitizenId) && !string.Equals(dto.CitizenId, existing.CitizenId, StringComparison.OrdinalIgnoreCase))
            {
                var existingCitizen = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.CitizenId == dto.CitizenId && u.Id != id && (u.IsDelete == false || u.IsDelete == null));
                if (existingCitizen != null)
                {
                    throw new ArgumentException("CCCD đã tồn tại. Vui lòng kiểm tra lại.");
                }
            }

            // QUAN TRỌNG: Lưu các giá trị quan trọng trước khi map để tránh bị mất
            var currentPassword = existing.Password;
            var currentUsername = existing.Username;
            var currentCode = existing.Code;
            var currentRoleId = existing.RoleId;
            var currentStatusCode = existing.StatusCode;

            _mapper.Map(dto, existing);
            
            // QUAN TRỌNG: Xử lý password riêng - không bao giờ để password bị null/empty
            // Nếu password là null, empty, hoặc là default value "123456", giữ lại password cũ
            // Chỉ update password nếu có password mới thực sự (khác default)
            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password == "123456")
            {
                // Không có password mới hoặc là default value - giữ lại password cũ
                existing.Password = currentPassword;
            }
            else
            {
                // Có password mới thực sự - hash và cập nhật
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dto.Password));
                    existing.Password = Convert.ToBase64String(hashedBytes);
                }
            }
            
            // Bảo vệ các field quan trọng khác
            if (string.IsNullOrWhiteSpace(dto.Username))
            {
                existing.Username = currentUsername;
            }
            
            // Code không có trong DTO, luôn giữ lại giá trị cũ
            existing.Code = currentCode;
            
            // RoleId và StatusCode không được thay đổi khi update từ danh sách người dùng
            existing.RoleId = currentRoleId;
            existing.StatusCode = currentStatusCode;
            
            // Explicitly set Address to ensure it's updated
            existing.Address = dto.Address;
            existing.LastModifiedDate = DateTime.UtcNow;

            await _repository.UpdateAsync(existing);
            return _mapper.Map<ResponseDto>(existing);
        }

        public async Task<int> GetTotalCountAsync(string? search = null, string? status = null, long? roleId = null)
        {
            return await _repository.GetTotalCountAsync(search, status, roleId);
        }

        /// <summary>
        /// ✅ Cập nhật Status của AutoOwner (ACTIVE hoặc INACTIVE)
        /// </summary>
        public async Task<ResponseDto?> UpdateStatusAsync(long id, string statusCode)
        {
            var autoOwner = await _repository.GetByIdAsync(id);
            if (autoOwner == null)
                throw new ArgumentException("Auto Owner not found");
            
            // Validate status
            if (statusCode != "ACTIVE" && statusCode != "INACTIVE")
                throw new ArgumentException($"Invalid status: {statusCode}. Only ACTIVE or INACTIVE is allowed.");
            
            autoOwner.StatusCode = statusCode;
            autoOwner.LastModifiedDate = DateTime.UtcNow;
            
            await _repository.UpdateAsync(autoOwner);
            
            return _mapper.Map<ResponseDto>(autoOwner);
        }

        // Sinh mã người dùng tự động: KH + 5 số tăng dần (KH00001, KH00002, ...)
        private async Task<string> GenerateUniqueAutoOwnerCodeAsync()
        {
            const string prefix = "KH";
            const int digits = 5;

            // Tìm mã lớn nhất hiện tại có prefix "KH"
            var maxCode = await _db.Users
                .AsNoTracking()
                .Where(u => u.Code != null && u.Code.StartsWith(prefix))
                .Select(u => u.Code)
                .ToListAsync();

            int nextNumber = 1;

            if (maxCode.Any())
            {
                // Lấy số lớn nhất từ các mã hiện có
                var numbers = maxCode
                    .Where(c => c.Length == prefix.Length + digits) // Đảm bảo format đúng
                    .Select(c =>
                    {
                        var numberPart = c.Substring(prefix.Length);
                        if (int.TryParse(numberPart, out int num))
                            return num;
                        return 0;
                    })
                    .Where(n => n > 0)
                    .ToList();

                if (numbers.Any())
                {
                    nextNumber = numbers.Max() + 1;
                }
            }

            // Format: KH + 5 số (KH00001, KH00002, ...)
            var code = $"{prefix}{nextNumber:D5}";

            // Double check để đảm bảo unique (trường hợp có race condition)
            var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Code == code);
            if (exists)
            {
                // Nếu trùng, tìm số tiếp theo
                nextNumber++;
                code = $"{prefix}{nextNumber:D5}";
            }

            return code;
        }
    }
}
