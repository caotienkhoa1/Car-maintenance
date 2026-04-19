using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BE.models;

namespace BE.repository.IRepository
{
    public interface ITotalReceiptRepository
    {
        Task<List<TotalReceipt>> GetListAsync(string? statusCode = null, long? branchId = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<TotalReceipt?> GetByIdAsync(long id);
        Task<TotalReceipt?> GetByMaintenanceTicketIdAsync(long maintenanceTicketId);
        Task<TotalReceipt> AddAsync(TotalReceipt entity);
        Task<TotalReceipt> UpdateAsync(TotalReceipt entity);
        Task<bool> DeleteAsync(long id);
        Task<bool> ExistsByMaintenanceTicketIdAsync(long maintenanceTicketId, long? excludeId = null);
    }
}


