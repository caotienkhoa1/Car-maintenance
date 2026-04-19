using BE.models;

namespace BE.repository.IRepository
{
    public interface IHistoryLogRepository
    {
        Task<HistoryLog> CreateAsync(HistoryLog historyLog);
        Task<List<HistoryLog>> GetByMaintenanceTicketIdAsync(long maintenanceTicketId);
    }
}


