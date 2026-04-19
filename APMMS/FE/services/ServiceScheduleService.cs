using FE.adapters;

namespace FE.services
{
    public class ServiceScheduleService
    {
        private readonly ApiAdapter _apiAdapter;

        public ServiceScheduleService(ApiAdapter apiAdapter)
        {
            _apiAdapter = apiAdapter;
        }

        public async Task<object?> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _apiAdapter.GetAsync<object>($"ServiceSchedule?page={page}&pageSize={pageSize}");
        }

        public async Task<object?> GetByIdAsync(long id)
        {
            return await _apiAdapter.GetAsync<object>($"ServiceSchedule/{id}");
        }

        public async Task<object?> GetByUserIdAsync(long userId)
        {
            return await _apiAdapter.GetAsync<object>($"ServiceSchedule/by-user/{userId}");
        }

        public async Task<object?> GetByBranchIdAsync(long branchId)
        {
            return await _apiAdapter.GetAsync<object>($"ServiceSchedule/by-branch/{branchId}");
        }

        public async Task<object?> GetByStatusAsync(string statusCode)
        {
            return await _apiAdapter.GetAsync<object>($"ServiceSchedule/by-status/{statusCode}");
        }

        public async Task<object?> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _apiAdapter.GetAsync<object>($"ServiceSchedule/by-date-range?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
        }

        public async Task<object?> GetTodaySchedulesAsync(long? branchId = null)
        {
            var url = "ServiceSchedule/today";
            if (branchId.HasValue)
            {
                url += $"?branchId={branchId.Value}";
            }
            return await _apiAdapter.GetAsync<object>(url);
        }

        public async Task<object?> GetTodaySchedulesCountAsync(long? branchId = null)
        {
            var url = "ServiceSchedule/today/count";
            if (branchId.HasValue)
            {
                url += $"?branchId={branchId.Value}";
            }
            return await _apiAdapter.GetAsync<object>(url);
        }

        public async Task<object?> CreateAsync(object payload)
        {
            return await _apiAdapter.PostAsync<object>("ServiceSchedule", payload);
        }

        public async Task<object?> UpdateAsync(long id, object payload)
        {
            return await _apiAdapter.PutAsync<object>($"ServiceSchedule/{id}", payload);
        }

        public async Task<object?> CancelAsync(long id, object? payload = null)
        {
            if (payload != null)
            {
                return await _apiAdapter.PutAsync<object>($"ServiceSchedule/{id}/cancel", payload);
            }
            else
            {
                return await _apiAdapter.PutAsync<object>($"ServiceSchedule/{id}/cancel", new { });
            }
        }

        public async Task<bool> DeleteAsync(long id)
        {
            return await _apiAdapter.DeleteAsync($"ServiceSchedule/{id}");
        }
    }
}

