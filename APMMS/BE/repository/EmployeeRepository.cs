using BE.models;
using BE.repository.IRepository;
using Microsoft.EntityFrameworkCore;


namespace BE.repository
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly CarMaintenanceDbContext _context;

        public EmployeeRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Branch)
                .Where(u => u.RoleId >= 3 && u.RoleId <= 6 && (u.IsDelete == null || u.IsDelete == false))
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(long id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Id == id && u.RoleId >= 3 && u.RoleId <= 6);
        }

        public async Task AddAsync(User employee)
        {
            await _context.Users.AddAsync(employee);
        }

        public async Task UpdateAsync(User employee)
        {
            _context.Users.Update(employee);
            await Task.CompletedTask;
        }

        public async Task SoftDeleteAsync(User employee)
        {
            employee.IsDelete = true;
            _context.Users.Update(employee);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public IQueryable<User> GetAll()
        {
            return _context.Users
                .Include(u => u.Role)
                .Include(u => u.Branch)
                .Where(u => u.RoleId >= 3 && u.RoleId <= 6);
        }

        public async Task<List<User>> GetWithFiltersAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null, long? roleId = null, long? branchId = null)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Branch)
                .Where(u => u.RoleId >= 2 && u.RoleId <= 6 && (u.IsDelete == false || u.IsDelete == null));

            // Filter theo role
            if (roleId.HasValue && roleId.Value >= 2 && roleId.Value <= 6)
            {
                query = query.Where(u => u.RoleId == roleId.Value);
            }

            // Filter theo branch
            if (branchId.HasValue && branchId.Value > 0)
            {
                query = query.Where(u => u.BranchId == branchId.Value);
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

            // Status filter - CHỈ theo StatusCode
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.ToUpper() == "ACTIVE")
                {
                    // ACTIVE = StatusCode là ACTIVE hoặc null (default là ACTIVE)
                    query = query.Where(u => u.StatusCode == "ACTIVE" || u.StatusCode == null);
                }
                else if (status.ToUpper() == "INACTIVE")
                {
                    // INACTIVE = StatusCode là INACTIVE
                    query = query.Where(u => u.StatusCode == "INACTIVE");
                }
            }

            return await query
                .OrderByDescending(u => u.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(string? search = null, string? status = null, long? roleId = null, long? branchId = null)
        {
            var query = _context.Users
                .Where(u => u.RoleId >= 2 && u.RoleId <= 6 && (u.IsDelete == false || u.IsDelete == null));

            // Filter theo role
            if (roleId.HasValue && roleId.Value >= 2 && roleId.Value <= 6)
            {
                query = query.Where(u => u.RoleId == roleId.Value);
            }

            // Filter theo branch
            if (branchId.HasValue && branchId.Value > 0)
            {
                query = query.Where(u => u.BranchId == branchId.Value);
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

            // Status filter - CHỈ theo StatusCode
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.ToUpper() == "ACTIVE")
                {
                    // ACTIVE = StatusCode là ACTIVE hoặc null (default là ACTIVE)
                    query = query.Where(u => u.StatusCode == "ACTIVE" || u.StatusCode == null);
                }
                else if (status.ToUpper() == "INACTIVE")
                {
                    // INACTIVE = StatusCode là INACTIVE
                    query = query.Where(u => u.StatusCode == "INACTIVE");
                }
            }

            return await query.CountAsync();
        }


    }
}

