using BE.repository.IRepository;
using BE.models;
using Microsoft.EntityFrameworkCore;

namespace BE.repository
{
    public class MaintenanceTicketRepository : IMaintenanceTicketRepository
    {
        private readonly CarMaintenanceDbContext _context;

        public MaintenanceTicketRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<MaintenanceTicket> CreateAsync(MaintenanceTicket maintenanceTicket)
        {
            _context.MaintenanceTickets.Add(maintenanceTicket);
            await _context.SaveChangesAsync();
            return maintenanceTicket;
        }

        public async Task<MaintenanceTicket?> GetByIdAsync(long id)
        {
            return await _context.MaintenanceTickets
                .Include(mt => mt.Car)
                    .ThenInclude(c => c.User)
                .Include(mt => mt.Consulter)
                .Include(mt => mt.Technician)
                .Include(mt => mt.Branch)
                .Include(mt => mt.ServiceCategory)
                .Include(mt => mt.ScheduleService)
                .Include(mt => mt.VehicleCheckin)
                    .ThenInclude(vc => vc.VehicleCheckinImages)
                .Include(mt => mt.ServicePackage)
                .Include(mt => mt.MaintenanceTicketTechnicians)
                    .ThenInclude(mtt => mtt.Technician)
                .FirstOrDefaultAsync(mt => mt.Id == id);
        }

        public async Task<MaintenanceTicket?> GetByIdWithBranchAsync(long id)
        {
            return await _context.MaintenanceTickets
                .Include(mt => mt.Branch)
                .FirstOrDefaultAsync(mt => mt.Id == id);
        }

        public async Task<MaintenanceTicket?> GetByIdWithCostDetailsAsync(long id)
        {
            return await _context.MaintenanceTickets
                .Include(mt => mt.TicketComponents)
                .Include(mt => mt.ServiceTasks)
                .FirstOrDefaultAsync(mt => mt.Id == id);
        }

        public async Task<List<MaintenanceTicket>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null)
        {
            System.Diagnostics.Debug.WriteLine($"[BE MaintenanceTicketRepository] GetAllAsync called with branchId: {branchId}");
            var query = _context.MaintenanceTickets
                .Include(mt => mt.Car)
                    .ThenInclude(c => c.User)
                .Include(mt => mt.Consulter)
                .Include(mt => mt.Technician)
                .Include(mt => mt.Branch)
                .AsQueryable();
            
            if (branchId.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"[BE MaintenanceTicketRepository] Filtering by branchId: {branchId.Value}");
                query = query.Where(mt => mt.BranchId == branchId.Value);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[BE MaintenanceTicketRepository] No branchId filter applied");
            }
            
            var result = await query
                .OrderByDescending(mt => mt.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            System.Diagnostics.Debug.WriteLine($"[BE MaintenanceTicketRepository] Returning {result.Count} tickets");
            return result;
        }

        public async Task<List<MaintenanceTicket>> GetByCarIdAsync(long carId)
        {
            return await _context.MaintenanceTickets
                .Include(mt => mt.Car)
                .Include(mt => mt.Consulter)
                .Include(mt => mt.Technician)
                .Include(mt => mt.Branch)
                .Where(mt => mt.CarId == carId)
                .OrderByDescending(mt => mt.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<MaintenanceTicket>> GetByUserIdAsync(long userId)
        {
            // ✅ Load đầy đủ dữ liệu từ TẤT CẢ chi nhánh, không filter theo branchId
            // Sử dụng AsSplitQuery để tránh vấn đề với multiple includes
            return await _context.MaintenanceTickets
                .AsSplitQuery() // ✅ Tách query để load đầy đủ dữ liệu
                .Include(mt => mt.Car)
                    .ThenInclude(c => c.User)
                .Include(mt => mt.Branch)
                .Include(mt => mt.ServiceCategory)
                .Include(mt => mt.TotalReceipts) // ✅ Load tất cả receipts của ticket
                .Where(mt => mt.Car != null && mt.Car.UserId == userId)
                .OrderByDescending(mt => mt.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<MaintenanceTicket>> GetByStatusAsync(string statusCode)
        {
            return await _context.MaintenanceTickets
                .Include(mt => mt.Car)
                .Include(mt => mt.Consulter)
                .Include(mt => mt.Technician)
                .Include(mt => mt.Branch)
                .Where(mt => mt.StatusCode == statusCode)
                .OrderByDescending(mt => mt.CreatedAt)
                .ToListAsync();
        }

        public async Task<MaintenanceTicket> UpdateAsync(MaintenanceTicket maintenanceTicket)
        {
            _context.MaintenanceTickets.Update(maintenanceTicket);
            await _context.SaveChangesAsync();
            return maintenanceTicket;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var maintenanceTicket = await _context.MaintenanceTickets.FindAsync(id);
            if (maintenanceTicket == null)
                return false;

            _context.MaintenanceTickets.Remove(maintenanceTicket);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.MaintenanceTickets.AnyAsync(mt => mt.Id == id);
        }

        public async Task<bool> CodeExistsAsync(string code)
        {
            return await _context.MaintenanceTickets.AnyAsync(mt => mt.Code == code);
        }
    }
}


