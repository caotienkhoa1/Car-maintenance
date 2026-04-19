using BE.models;
using BE.repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace BE.repository
{
    public class AutoOwnerRepository : IAutoOwnerRepository
    {
        private readonly CarMaintenanceDbContext _context;

        public AutoOwnerRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Users
                .Include(u => u.Cars)
                .Include(u => u.Branch)
                .Include(u => u.Role)
                .Where(u => (u.IsDelete == false || u.IsDelete == null) &&
                            u.Role != null && u.Role.Name == "Auto Owner")
                .OrderByDescending(u => u.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<User>> GetWithFiltersAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null, long? roleId = null)
        {
            var query = _context.Users
                .Include(u => u.Cars)
                .Include(u => u.Role)
                .Include(u => u.Branch)
                .Where(u => (u.IsDelete == false || u.IsDelete == null));

            // Luôn filter theo Auto Owner (roleId = 7 hoặc Role.Name == "Auto Owner")
            // Nếu roleId được chỉ định, chỉ dùng nếu là 7 (Auto Owner)
            if (roleId.HasValue && roleId.Value == 7)
            {
                query = query.Where(u => u.RoleId == 7);
            }
            else
            {
                // Luôn chỉ lấy Auto Owner
                query = query.Where(u => u.Role != null && (u.Role.Name == "Auto Owner" || u.RoleId == 7));
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(u =>
                    (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(searchLower)) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                    (u.Phone != null && u.Phone.Contains(search)) ||
                    (u.Username != null && u.Username.ToLower().Contains(searchLower))
                );
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.ToUpper() == "ACTIVE")
                {
                    query = query.Where(u => u.StatusCode == "ACTIVE" || (u.StatusCode == null && u.IsDelete == false));
                }
                else if (status.ToUpper() == "INACTIVE")
                {
                    query = query.Where(u => u.StatusCode == "INACTIVE" || u.IsDelete == true);
                }
            }

            return await query
                .OrderByDescending(u => u.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(string? search = null, string? status = null, long? roleId = null)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Branch)
                .Where(u => (u.IsDelete == false || u.IsDelete == null));

            // Filter theo role Auto Owner (roleId = 7) hoặc role được chỉ định
            if (roleId.HasValue)
            {
                query = query.Where(u => u.RoleId == roleId.Value);
            }
            else
            {
                // Luôn filter theo Auto Owner nếu không có roleId
                query = query.Where(u => u.RoleId == 7 || (u.Role != null && u.Role.Name == "Auto Owner"));
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(u =>
                    (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(searchLower)) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                    (u.Phone != null && u.Phone.Contains(search)) ||
                    (u.Username != null && u.Username.ToLower().Contains(searchLower))
                );
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.ToUpper() == "ACTIVE")
                {
                    query = query.Where(u => u.StatusCode == "ACTIVE" || (u.StatusCode == null && u.IsDelete == false));
                }
                else if (status.ToUpper() == "INACTIVE")
                {
                    query = query.Where(u => u.StatusCode == "INACTIVE" || u.IsDelete == true);
                }
            }

            return await query.CountAsync();
        }

        public async Task<User?> GetByIdAsync(long id)
        {
            return await _context.Users
                .Include(u => u.Cars)
                .Include(u => u.Role)
                 .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Id == id && (u.IsDelete == false || u.IsDelete == null));
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && (u.IsDelete == false || u.IsDelete == null));
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && (u.IsDelete == false || u.IsDelete == null));
        }

        public async Task<User?> GetByPhoneAsync(string phone)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Phone == phone && (u.IsDelete == false || u.IsDelete == null));
        }

        public async Task<User?> GetByCitizenIdAsync(string citizenId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.CitizenId == citizenId && (u.IsDelete == false || u.IsDelete == null));
        }

        public async Task<List<User>> GetByCarIdAsync(long carId)
        {
            return await _context.Users
                .Include(u => u.Cars)
                .Where(u => (u.IsDelete == false || u.IsDelete == null) && u.Cars.Any(c => c.Id == carId))
                .ToListAsync();
        }

        public async Task<List<User>> GetByBranchIdAsync(long branchId)
        {
            return await _context.Users
                .Include(u => u.Cars)
                .Where(u => (u.IsDelete == false || u.IsDelete == null) && u.BranchId == branchId)
                .ToListAsync();
        }

        public async Task<User> CreateAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.IsDelete = true;
            user.LastModifiedDate = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

