using FE.adapters;

namespace FE.services
{
    public class MaintenanceTicketService
    {
        private readonly ApiAdapter _apiAdapter;

        public MaintenanceTicketService(ApiAdapter apiAdapter)
        {
            _apiAdapter = apiAdapter;
        }

        public async Task<object?> GetAllAsync(int page, int pageSize, long? branchId = null)
        {
            var url = $"MaintenanceTicket?page={page}&pageSize={pageSize}";
            if (branchId.HasValue)
            {
                url += $"&branchId={branchId.Value}";
                Console.WriteLine($"[FE MaintenanceTicketService] Calling API with branchId: {branchId.Value}");
            }
            else
            {
                Console.WriteLine("[FE MaintenanceTicketService] Calling API without branchId filter");
            }
            return await _apiAdapter.GetAsync<object>(url);
        }

        public async Task<object?> CreateFromCheckinAsync(object payload)
        {
            return await _apiAdapter.PostAsync<object>("MaintenanceTicket/create-from-checkin", payload);
        }
    }
}


