using AutoMapper;
using BE.DTOs.Employee;
using BE.interfaces;
using BE.models;
using BE.repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace BE.services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IMapper _mapper;
        private readonly CarMaintenanceDbContext _db;
        private readonly IHistoryLogRepository _historyLogRepository;

        public EmployeeService(IEmployeeRepository employeeRepository, IMapper mapper, CarMaintenanceDbContext db, IHistoryLogRepository historyLogRepository)
        {
            _employeeRepository = employeeRepository;
            _mapper = mapper;
            _db = db;
            _historyLogRepository = historyLogRepository;
        }

        public async Task<IEnumerable<EmployeeResponseDto>> GetAllAsync()
        {
            var employees = await _employeeRepository.GetAllAsync();
            return employees.Select(e => _mapper.Map<EmployeeResponseDto>(e));
        }

        public async Task<EmployeeResponseDto?> GetByIdAsync(long id)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee == null) return null;
            return _mapper.Map<EmployeeResponseDto>(employee);
        }

        // ✅ CHỈNH Ở ĐÂY: tự sinh Code theo Role
        public async Task<EmployeeResponseDto> CreateAsync(EmployeeRequestDto dto, long? createdByUserId = null)
        {
            // Validate business rules
            await ValidateEmployeeDataAsync(dto, null);

            var employee = _mapper.Map<User>(dto);

            // Sinh mã tự động nếu chưa có
            var prefix = await GetRolePrefixAsync(dto.RoleId);
            employee.Code = await GenerateUniqueEmployeeCodeAsync(prefix);

            employee.CreatedDate = DateTime.Now;
            employee.IsDelete = false;
            employee.CreatedBy = createdByUserId;
            
            // ✅ Set StatusCode default là ACTIVE nếu không được set
            employee.StatusCode = string.IsNullOrWhiteSpace(dto.StatusCode) ? "ACTIVE" : dto.StatusCode;

            try
            {
                await _employeeRepository.AddAsync(employee);
                await _employeeRepository.SaveChangesAsync();

                // Create audit log
                if (createdByUserId.HasValue)
                {
                    await CreateAuditLogAsync(
                        createdByUserId.Value,
                        null,
                        employee.Id,
                        "CREATE_EMPLOYEE",
                        null,
                        JsonSerializer.Serialize(new { 
                            Code = employee.Code,
                            Username = employee.Username,
                            FirstName = employee.FirstName,
                            LastName = employee.LastName,
                            RoleId = employee.RoleId,
                            BranchId = employee.BranchId,
                            StatusCode = employee.StatusCode
                        })
                    );
                }
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
            {
                // Check if it's a username unique constraint violation - this is the username constraint
                throw new ArgumentException($"Tên đăng nhập '{dto.Username}' đã tồn tại. Vui lòng chọn tên khác.");
            }

            return _mapper.Map<EmployeeResponseDto>(employee);
        }

        public async Task<EmployeeResponseDto?> UpdateAsync(long id, EmployeeRequestDto dto, long? modifiedByUserId = null)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee == null) return null;

            // Validate business rules (pass current employee ID to check username uniqueness)
            await ValidateEmployeeDataAsync(dto, id);

            // Store current values before mapping to preserve them if they are null/empty
            var oldData = new
            {
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                Phone = employee.Phone,
                RoleId = employee.RoleId,
                BranchId = employee.BranchId,
                StatusCode = employee.StatusCode,
                CitizenId = employee.CitizenId,
                TaxCode = employee.TaxCode,
                Address = employee.Address
            };

            var currentPassword = employee.Password;
            var currentRoleId = employee.RoleId;
            var currentBranchId = employee.BranchId;
            
            _mapper.Map(dto, employee);
            
            // If password is empty or whitespace, keep the original password (don't update password)
            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                employee.Password = currentPassword;
            }
            
            // If roleId is null, keep the original roleId
            if (dto.RoleId == null || dto.RoleId == 0)
            {
                employee.RoleId = currentRoleId;
            }
            
            // If branchId is null, keep the original branchId
            if (dto.BranchId == null || dto.BranchId == 0)
            {
                employee.BranchId = currentBranchId;
            }
            
            employee.LastModifiedDate = DateTime.Now;
            employee.LastModifiedBy = modifiedByUserId;

            try
            {
                await _employeeRepository.UpdateAsync(employee);
                await _employeeRepository.SaveChangesAsync();

                // Create audit log for important changes
                if (modifiedByUserId.HasValue)
                {
                    var newData = new
                    {
                        FirstName = employee.FirstName,
                        LastName = employee.LastName,
                        Email = employee.Email,
                        Phone = employee.Phone,
                        RoleId = employee.RoleId,
                        BranchId = employee.BranchId,
                        StatusCode = employee.StatusCode,
                        CitizenId = employee.CitizenId,
                        TaxCode = employee.TaxCode,
                        Address = employee.Address
                    };

                    // Only log if there are actual changes
                    if (JsonSerializer.Serialize(oldData) != JsonSerializer.Serialize(newData))
                    {
                        await CreateAuditLogAsync(
                            modifiedByUserId.Value,
                            null,
                            employee.Id,
                            "UPDATE_EMPLOYEE",
                            JsonSerializer.Serialize(oldData),
                            JsonSerializer.Serialize(newData)
                        );
                    }
                }
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
            {
                // Check if it's a username unique constraint violation - this is the username constraint
                throw new ArgumentException($"Tên đăng nhập '{dto.Username}' đã tồn tại. Vui lòng chọn tên khác.");
            }

            return _mapper.Map<EmployeeResponseDto>(employee);
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee == null) return false;

            await _employeeRepository.SoftDeleteAsync(employee);
            await _employeeRepository.SaveChangesAsync();
            return true;
        }

        // 🆕 Lấy prefix theo Role (ưu tiên theo Role.Name trong DB)
        private async Task<string> GetRolePrefixAsync(long? roleId)
        {
            if (roleId == null) return "EM"; // fallback

            var role = await _db.Roles.AsNoTracking()
                                      .Where(r => r.Id == roleId.Value)
                                      .Select(r => r.Name)
                                      .FirstOrDefaultAsync();

            var name = role?.Trim().ToLowerInvariant() ?? string.Empty;

            return name switch
            {
                "consulter" => "CL",
                "accountant" => "AC",
                "technician" => "TC",
                "warehouse keeper" => "WK",
                "branch manager" => "BM",
                "director" => "DR",
                "manager" => "MG",
                _ => "EM"
            };
        }

        // 🆕 Sinh mã duy nhất: PREFIX + 5 số ngẫu nhiên (vd: CL04218)
        private async Task<string> GenerateUniqueEmployeeCodeAsync(string prefix)
        {
            const int digits = 5;
            var rnd = new Random();

            for (int i = 0; i < 50; i++)
            {
                var number = rnd.Next(0, (int)Math.Pow(10, digits)).ToString($"D{digits}");
                var code = $"{prefix}{number}";

                var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Code == code);
                if (!exists) return code;
            }

            // Fallback (hầu như không bao giờ xảy ra)
            return $"{prefix}{DateTime.UtcNow:HHmmssff}";
        }

        public async Task<IEnumerable<EmployeeResponseDto>> GetAllAsync(string? status = null, int? roleId = null, string? roleName = null)
        {
            var query = _employeeRepository.GetAll()
                .Include(e => e.Role)
                .Include(e => e.Branch)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (status.ToLower() == "active")
                    query = query.Where(e => e.IsDelete == false);
                else if (status.ToLower() == "inactive")
                    query = query.Where(e => e.IsDelete == true);
            }

            if (roleId.HasValue)
                query = query.Where(e => e.RoleId == roleId.Value);

            if (!string.IsNullOrEmpty(roleName))
                query = query.Where(e => e.Role.Name.ToLower().Contains(roleName.ToLower()));

            var employees = await query.ToListAsync();
            return _mapper.Map<IEnumerable<EmployeeResponseDto>>(employees);
        }

        public async Task<IEnumerable<EmployeeResponseDto>> FilterAsync(bool? isDelete, long? roleId, long? branchId = null)
        {
            var query = _employeeRepository.GetAll();

            if (isDelete.HasValue)
                query = query.Where(e => e.IsDelete == isDelete.Value);

            if (roleId.HasValue)
                query = query.Where(e => e.RoleId == roleId.Value);

            if (branchId.HasValue)
                query = query.Where(e => e.BranchId == branchId.Value);

            var employees = await query.ToListAsync();
            return employees.Select(e => _mapper.Map<EmployeeResponseDto>(e));
        }

        public async Task<EmployeeProfileDto?> GetProfileAsync(long userId)
        {
            var employee = await _employeeRepository.GetByIdAsync(userId);
            if (employee == null) return null;

            var dto = new EmployeeProfileDto
            {
                Username = employee.Username,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                Phone = employee.Phone,
                Gender = employee.Gender,
                Image = employee.Image,
                BranchName = employee.Branch?.Name,
                RoleName = employee.Role?.Name
            };

            return dto;
        }

        public async Task<EmployeeProfileDto?> UpdateProfileAsync(long userId, UpdateProfileDto dto)
        {
            var employee = await _employeeRepository.GetByIdAsync(userId);
            if (employee == null) return null;

            if (!string.IsNullOrEmpty(dto.FirstName)) employee.FirstName = dto.FirstName;
            if (!string.IsNullOrEmpty(dto.LastName)) employee.LastName = dto.LastName;
            if (!string.IsNullOrEmpty(dto.Email)) employee.Email = dto.Email;
            if (!string.IsNullOrEmpty(dto.Phone)) employee.Phone = dto.Phone;
            if (!string.IsNullOrEmpty(dto.Gender)) employee.Gender = dto.Gender;
            if (!string.IsNullOrEmpty(dto.Image)) employee.Image = dto.Image;
            employee.LastModifiedDate = DateTime.Now;

            await _employeeRepository.UpdateAsync(employee);
            await _employeeRepository.SaveChangesAsync();

            return await GetProfileAsync(userId);
        }

        public async Task<object> GetWithFiltersAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null, long? roleId = null, long? branchId = null)
        {
            var employees = await _employeeRepository.GetWithFiltersAsync(page, pageSize, search, status, roleId, branchId);
            var totalCount = await _employeeRepository.GetTotalCountAsync(search, status, roleId, branchId);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            
            return new
            {
                success = true,
                data = _mapper.Map<List<EmployeeResponseDto>>(employees),
                page = page,
                pageSize = pageSize,
                totalPages = totalPages,
                currentPage = page,
                totalCount = totalCount
            };
        }
        
        /// <summary>
        /// ✅ Cập nhật Status của Employee (ACTIVE hoặc INACTIVE)
        /// </summary>
        public async Task<EmployeeResponseDto?> UpdateStatusAsync(long id, string statusCode)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee == null)
                throw new ArgumentException("Employee not found");
            
            // Validate status
            if (statusCode != "ACTIVE" && statusCode != "INACTIVE")
                throw new ArgumentException($"Invalid status: {statusCode}. Only ACTIVE or INACTIVE is allowed.");
            
            employee.StatusCode = statusCode;
            employee.LastModifiedDate = DateTime.Now;
            
            await _employeeRepository.UpdateAsync(employee);
            await _employeeRepository.SaveChangesAsync();
            
            return _mapper.Map<EmployeeResponseDto>(employee);
        }

        /// <summary>
        /// Validate employee data before create/update
        /// </summary>
        private async Task ValidateEmployeeDataAsync(EmployeeRequestDto dto, long? currentEmployeeId)
        {
            // Validate RoleId exists
            if (dto.RoleId.HasValue && dto.RoleId.Value > 0)
            {
                var roleExists = await _db.Roles.AsNoTracking()
                    .AnyAsync(r => r.Id == dto.RoleId.Value);
                if (!roleExists)
                {
                    throw new ArgumentException($"RoleId {dto.RoleId.Value} không tồn tại trong hệ thống.");
                }

                // Validate role is employee role (3-6)
                if (dto.RoleId.Value < 3 || dto.RoleId.Value > 6)
                {
                    throw new ArgumentException($"RoleId {dto.RoleId.Value} không phải là role nhân viên hợp lệ (phải từ 3-6).");
                }
            }

            // Validate BranchId exists
            if (dto.BranchId.HasValue && dto.BranchId.Value > 0)
            {
                var branchExists = await _db.Branches.AsNoTracking()
                    .AnyAsync(b => b.Id == dto.BranchId.Value);
                if (!branchExists)
                {
                    throw new ArgumentException($"BranchId {dto.BranchId.Value} không tồn tại trong hệ thống.");
                }
            }

            // Validate Username uniqueness (check before save to avoid exception)
            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                var usernameExists = await _db.Users.AsNoTracking()
                    .AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower() && 
                                   (currentEmployeeId == null || u.Id != currentEmployeeId.Value));
                if (usernameExists)
                {
                    throw new ArgumentException($"Tên đăng nhập '{dto.Username}' đã tồn tại. Vui lòng chọn tên khác.");
                }
            }

            // Validate Email format (if provided)
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                try
                {
                    var email = new System.Net.Mail.MailAddress(dto.Email);
                    if (email.Address != dto.Email)
                    {
                        throw new ArgumentException("Email không hợp lệ.");
                    }
                }
                catch
                {
                    throw new ArgumentException("Email không hợp lệ.");
                }
            }

            // Validate Phone format (if provided) - Vietnamese phone: 10 digits starting with 0
            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                // Remove spaces and dashes
                var phone = dto.Phone.Replace(" ", "").Replace("-", "");
                if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0[3|5|7|8|9][0-9]{8}$"))
                {
                    throw new ArgumentException("Số điện thoại không hợp lệ. Phải bắt đầu bằng 0 và có 10 chữ số.");
                }
            }

            // Validate CitizenId format (if provided) - 12 digits only
            if (!string.IsNullOrWhiteSpace(dto.CitizenId))
            {
                var citizenId = dto.CitizenId.Replace(" ", "").Replace("-", "");
                if (!System.Text.RegularExpressions.Regex.IsMatch(citizenId, @"^[0-9]{12}$"))
                {
                    throw new ArgumentException("CMT/CCCD phải có đúng 12 chữ số.");
                }
            }

            // Validate TaxCode format (if provided) - 10 digits
            if (!string.IsNullOrWhiteSpace(dto.TaxCode))
            {
                var taxCode = dto.TaxCode.Replace(" ", "").Replace("-", "");
                if (!System.Text.RegularExpressions.Regex.IsMatch(taxCode, @"^[0-9]{10}$"))
                {
                    throw new ArgumentException("Mã số thuế phải có đúng 10 chữ số.");
                }
            }

            // Validate DOB (if provided) - must be at least 18 years old
            if (!string.IsNullOrWhiteSpace(dto.Dob))
            {
                try
                {
                    var dobParts = dto.Dob.Split('-');
                    if (dobParts.Length == 3)
                    {
                        var day = int.Parse(dobParts[0]);
                        var month = int.Parse(dobParts[1]);
                        var year = int.Parse(dobParts[2]);
                        var dob = new DateOnly(year, month, day);
                        var today = DateOnly.FromDateTime(DateTime.Today);
                        var age = today.Year - dob.Year;
                        if (dob > today.AddYears(-age)) age--;

                        if (age < 18)
                        {
                            throw new ArgumentException("Nhân viên phải đủ 18 tuổi trở lên.");
                        }

                        // Check if DOB is not in the future
                        if (dob > today)
                        {
                            throw new ArgumentException("Ngày sinh không được ở tương lai.");
                        }
                    }
                }
                catch (ArgumentException)
                {
                    throw;
                }
                catch
                {
                    throw new ArgumentException("Ngày sinh không hợp lệ. Định dạng phải là dd-MM-yyyy.");
                }
            }

            // Validate StatusCode (if provided)
            if (!string.IsNullOrWhiteSpace(dto.StatusCode))
            {
                if (dto.StatusCode != "ACTIVE" && dto.StatusCode != "INACTIVE")
                {
                    throw new ArgumentException("Trạng thái chỉ được là ACTIVE hoặc INACTIVE.");
                }
            }
        }

        /// <summary>
        /// Create audit log entry
        /// </summary>
        private async Task CreateAuditLogAsync(long userId, long? maintenanceTicketId, long? targetEmployeeId, string action, string? oldData, string? newData)
        {
            try
            {
                var historyLog = new HistoryLog
                {
                    UserId = userId,
                    MaintenanceTicketId = maintenanceTicketId,
                    Action = action,
                    OldData = oldData,
                    NewData = newData,
                    CreatedAt = DateTime.Now
                };

                // Note: We can't use targetEmployeeId directly in HistoryLog model as it doesn't have that field
                // But we can include it in the NewData/OldData JSON
                await _historyLogRepository.CreateAsync(historyLog);
            }
            catch
            {
                // Don't throw exception if audit log fails - it's not critical
                // Just log to console or use ILogger in production
            }
        }
    }
}


