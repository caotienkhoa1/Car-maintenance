using BE.models;

namespace BE.repository.IRepository
{
    public interface IServiceScheduleRepository
    {
        Task<ScheduleService> CreateAsync(ScheduleService scheduleService);
        Task<ScheduleService?> GetByIdAsync(long id);
        Task<List<ScheduleService>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<List<ScheduleService>> GetByUserIdAsync(long userId);
        Task<List<ScheduleService>> GetByBranchIdAsync(long branchId);
        Task<List<ScheduleService>> GetByStatusAsync(string statusCode, long? branchId = null);
        Task<List<ScheduleService>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, long? branchId = null);
        Task<ScheduleService> UpdateAsync(ScheduleService scheduleService);
        Task<bool> DeleteAsync(long id);
        Task<bool> ExistsAsync(long id);
    }
}
