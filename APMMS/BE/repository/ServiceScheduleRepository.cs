using BE.models;
using BE.repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BE.repository
{
    public class ServiceScheduleRepository : IServiceScheduleRepository
    {
        private readonly CarMaintenanceDbContext _context;

        public ServiceScheduleRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<ScheduleService> CreateAsync(ScheduleService scheduleService)
        {
            _context.ScheduleServices.Add(scheduleService);
            await _context.SaveChangesAsync();
            return scheduleService;
        }

        public async Task<ScheduleService?> GetByIdAsync(long id)
        {
            return await _context.ScheduleServices
                .Include(s => s.User)
                .Include(s => s.Guest)
                .Include(s => s.Car)
                    .ThenInclude(c => c.User)
                .Include(s => s.Branch)
                .Include(s => s.ServiceCategory)
                .Include(s => s.StatusCodeNavigation)
                .Include(s => s.ScheduleServiceNotes)
                    .ThenInclude(n => n.Consultant)
                        .ThenInclude(c => c.Branch)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<ScheduleService>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _context.ScheduleServices
                .Include(s => s.User)
                .Include(s => s.Guest)
                .Include(s => s.Car)
                    .ThenInclude(c => c.User)
                .Include(s => s.Branch)
                .Include(s => s.StatusCodeNavigation)
                .Include(s => s.ScheduleServiceNotes)
                    .ThenInclude(n => n.Consultant)
                .OrderByDescending(s => s.ScheduledDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<ScheduleService>> GetByUserIdAsync(long userId)
        {
            return await _context.ScheduleServices
                .Include(s => s.User)
                .Include(s => s.Guest)
                .Include(s => s.Car)
                    .ThenInclude(c => c.User)
                .Include(s => s.Branch)
                .Include(s => s.StatusCodeNavigation)
                .Include(s => s.ScheduleServiceNotes)
                    .ThenInclude(n => n.Consultant)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.ScheduledDate)
                .ToListAsync();
        }

        public async Task<List<ScheduleService>> GetByBranchIdAsync(long branchId)
        {
            return await _context.ScheduleServices
                .Include(s => s.User)
                .Include(s => s.Guest)
                .Include(s => s.Car)
                    .ThenInclude(c => c.User)
                .Include(s => s.Branch)
                .Include(s => s.StatusCodeNavigation)
                .Include(s => s.ScheduleServiceNotes)
                    .ThenInclude(n => n.Consultant)
                .Where(s => s.BranchId == branchId)
                .OrderByDescending(s => s.ScheduledDate)
                .ToListAsync();
        }

        public async Task<List<ScheduleService>> GetByStatusAsync(string statusCode, long? branchId = null)
        {
            var query = _context.ScheduleServices
                .Include(s => s.User)
                .Include(s => s.Guest)
                .Include(s => s.Car)
                    .ThenInclude(c => c.User)
                .Include(s => s.Branch)
                .Include(s => s.StatusCodeNavigation)
                .Include(s => s.ScheduleServiceNotes)
                    .ThenInclude(n => n.Consultant)
                .Where(s => s.StatusCode == statusCode);

            // Filter theo branchId nếu có
            if (branchId.HasValue)
            {
                query = query.Where(s => s.BranchId == branchId.Value);
            }

            return await query
                .OrderByDescending(s => s.ScheduledDate)
                .ToListAsync();
        }

        public async Task<List<ScheduleService>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, long? branchId = null)
        {
            var query = _context.ScheduleServices
                .Include(s => s.User)
                .Include(s => s.Guest)
                .Include(s => s.Car)
                    .ThenInclude(c => c.User)
                .Include(s => s.Branch)
                .Include(s => s.StatusCodeNavigation)
                .Include(s => s.ScheduleServiceNotes)
                    .ThenInclude(n => n.Consultant)
                .Where(s => s.ScheduledDate >= startDate && s.ScheduledDate <= endDate);

            // Filter theo branchId nếu có
            if (branchId.HasValue)
            {
                query = query.Where(s => s.BranchId == branchId.Value);
            }

            return await query
                .OrderByDescending(s => s.ScheduledDate)
                .ToListAsync();
        }

        public async Task<ScheduleService> UpdateAsync(ScheduleService scheduleService)
        {
            // Tìm entity trong database bằng FindAsync (nhanh hơn và không load navigation properties)
            var existing = await _context.ScheduleServices.FindAsync(scheduleService.Id);
            
            if (existing == null)
            {
                throw new InvalidOperationException($"Schedule with ID {scheduleService.Id} not found");
            }

            // Chỉ update các properties thay đổi, không động đến navigation properties
            if (scheduleService.StatusCode != null)
            {
                existing.StatusCode = scheduleService.StatusCode;
            }
            
            if (scheduleService.ScheduledDate != default(DateTime))
            {
                existing.ScheduledDate = scheduleService.ScheduledDate;
            }
            
            if (scheduleService.BranchId.HasValue)
            {
                existing.BranchId = scheduleService.BranchId.Value;
            }
            
            if (scheduleService.ServiceCategoryId.HasValue)
            {
                existing.ServiceCategoryId = scheduleService.ServiceCategoryId.Value;
            }

            // Update GuestId và UserId nếu có thay đổi
            if (scheduleService.GuestId.HasValue)
            {
                existing.GuestId = scheduleService.GuestId.Value;
                // Nếu set GuestId, thì UserId phải là null (theo CHECK constraint)
                existing.UserId = null;
            }
            
            if (scheduleService.UserId.HasValue)
            {
                existing.UserId = scheduleService.UserId.Value;
                // Nếu set UserId, thì GuestId phải là null (theo CHECK constraint)
                existing.GuestId = null;
            }
            
            // Update audit fields if provided
            if (scheduleService.UpdatedAt.HasValue)
            {
                existing.UpdatedAt = scheduleService.UpdatedAt;
            }
            
            if (scheduleService.UpdatedBy.HasValue)
            {
                existing.UpdatedBy = scheduleService.UpdatedBy;
            }

            // Save changes - chỉ update các properties đã thay đổi
            await _context.SaveChangesAsync();
            
            // Reload entity với đầy đủ navigation properties để trả về
            // Detach entity hiện tại trước để tránh conflict
            _context.Entry(existing).State = EntityState.Detached;
            
            // Reload với đầy đủ navigation properties
            var updated = await GetByIdAsync(scheduleService.Id);
            return updated!;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var schedule = await _context.ScheduleServices.FindAsync(id);
            if (schedule == null)
                return false;

            _context.ScheduleServices.Remove(schedule);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.ScheduleServices.AnyAsync(s => s.Id == id);
        }
    }
}
