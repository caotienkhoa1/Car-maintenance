using BE.repository.IRepository;
using BE.models;
using Microsoft.EntityFrameworkCore;

namespace BE.repository
{
    public class ServiceTaskRepository : IServiceTaskRepository
    {
        private readonly CarMaintenanceDbContext _context;

        public ServiceTaskRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceTask> CreateAsync(ServiceTask serviceTask)
        {
            _context.ServiceTasks.Add(serviceTask);
            await _context.SaveChangesAsync();
            return serviceTask;
        }

        public async Task<ServiceTask?> GetByIdAsync(long id)
        {
            return await _context.ServiceTasks
                .Include(st => st.MaintenanceTicket)
                .FirstOrDefaultAsync(st => st.Id == id);
        }

        public async Task<ServiceTask?> GetByIdWithDetailsAsync(long id)
        {
            return await _context.ServiceTasks
                .Include(st => st.ServiceCategory)
                .Include(st => st.Technician) // ✅ Include Technician trực tiếp từ ServiceTask
                .Include(st => st.ServiceTaskTechnicians) // ✅ Include nhiều kỹ thuật viên
                    .ThenInclude(stt => stt.Technician)
                .Include(st => st.MaintenanceTicket)
                    .ThenInclude(mt => mt!.Car)
                        .ThenInclude(c => c!.User)
                .Include(st => st.MaintenanceTicket)
                    .ThenInclude(mt => mt!.Technician)
                .Include(st => st.MaintenanceTicket)
                    .ThenInclude(mt => mt!.Branch)
                .FirstOrDefaultAsync(st => st.Id == id);
        }

        public async Task<List<ServiceTask>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _context.ServiceTasks
                .Include(st => st.MaintenanceTicket)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<ServiceTask>> GetAllWithDetailsAsync(int page = 1, int pageSize = 10)
        {
            return await _context.ServiceTasks
                .Include(st => st.MaintenanceTicket)
                    .ThenInclude(mt => mt!.Car)
                        .ThenInclude(c => c!.User)
                .Include(st => st.MaintenanceTicket)
                    .ThenInclude(mt => mt!.Technician)
                .Include(st => st.MaintenanceTicket)
                    .ThenInclude(mt => mt!.Branch)
                .OrderByDescending(st => st.Id) // ✅ Sắp xếp nhiệm vụ mới nhất lên trên
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<ServiceTask>> GetByMaintenanceTicketIdAsync(long maintenanceTicketId)
        {
            return await _context.ServiceTasks
                .Include(st => st.ServiceCategory)
                .Include(st => st.Technician) // ✅ Include Technician trực tiếp từ ServiceTask
                .Include(st => st.ServiceTaskTechnicians) // ✅ Include nhiều kỹ thuật viên
                    .ThenInclude(stt => stt.Technician)
                .Include(st => st.MaintenanceTicket)
                    .ThenInclude(mt => mt!.Car)
                        .ThenInclude(c => c!.User)
                .Include(st => st.MaintenanceTicket)
                    .ThenInclude(mt => mt!.Technician)
                .Include(st => st.MaintenanceTicket)
                    .ThenInclude(mt => mt!.Branch)
                .Where(st => st.MaintenanceTicketId == maintenanceTicketId)
                .OrderBy(st => st.DisplayOrder) // ✅ Sắp xếp theo DisplayOrder
                .ThenByDescending(st => st.Id) // Nếu DisplayOrder giống nhau, sắp xếp theo ID
                .ToListAsync();
        }

        public async Task<List<ServiceTask>> GetByStatusAsync(string statusCode)
        {
            return await _context.ServiceTasks
                .Include(st => st.MaintenanceTicket)
                    .ThenInclude(mt => mt!.Car)
                        .ThenInclude(c => c!.User)
                .Include(st => st.MaintenanceTicket)
                    .ThenInclude(mt => mt!.Technician)
                .Include(st => st.MaintenanceTicket)
                    .ThenInclude(mt => mt!.Branch)
                .Where(st => st.StatusCode == statusCode)
                .OrderByDescending(st => st.Id) // ✅ Sắp xếp nhiệm vụ mới nhất lên trên
                .ToListAsync();
        }

        public async Task<List<ServiceTask>> GetByTechnicianIdAsync(long technicianId)
        {
            // ✅ CHỈ lấy ServiceTasks mà technician này được gán TRỰC TIẾP (từ ServiceTaskTechnicians)
            // KHÔNG lấy từ MaintenanceTicketTechnicians (gán ở "Thao tác")
            return await _context.ServiceTasks
                .Include(st => st.ServiceCategory)
                .Include(st => st.Technician)
                .Include(st => st.ServiceTaskTechnicians)
                    .ThenInclude(stt => stt.Technician)
                .Include(st => st.MaintenanceTicket)
                    .ThenInclude(mt => mt!.Car)
                        .ThenInclude(c => c!.User)
                .Include(st => st.MaintenanceTicket)
                    .ThenInclude(mt => mt!.Technician)
                .Include(st => st.MaintenanceTicket)
                    .ThenInclude(mt => mt!.Branch)
                .Where(st => st.ServiceTaskTechnicians.Any(stt => stt.TechnicianId == technicianId))
                .OrderByDescending(st => st.Id) // ✅ Sắp xếp nhiệm vụ mới nhất lên trên
                .ToListAsync();
        }

        public async Task<ServiceTask> UpdateAsync(ServiceTask serviceTask)
        {
            _context.ServiceTasks.Update(serviceTask);
            await _context.SaveChangesAsync();
            return serviceTask;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var serviceTask = await _context.ServiceTasks.FindAsync(id);
            if (serviceTask == null)
                return false;

            _context.ServiceTasks.Remove(serviceTask);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.ServiceTasks.AnyAsync(st => st.Id == id);
        }
    }
}