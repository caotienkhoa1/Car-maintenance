using FE.adapters;

namespace FE.services
{
    public class VehicleCheckinService
    {
        private readonly ApiAdapter _apiAdapter;

        public VehicleCheckinService(ApiAdapter apiAdapter)
        {
            _apiAdapter = apiAdapter;
        }

        public async Task<object?> GetAllAsync(int page, int pageSize, string? searchTerm = null, string? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(searchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");

            if (!string.IsNullOrWhiteSpace(statusCode))
                queryParams.Add($"statusCode={Uri.EscapeDataString(statusCode)}");

            if (fromDate.HasValue)
                queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");

            if (toDate.HasValue)
                queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

            // BE sẽ tự động lấy branchId từ JWT token, không cần gửi từ FE
            var queryString = string.Join("&", queryParams);
            return await _apiAdapter.GetAsync<object>($"VehicleCheckin?{queryString}");
        }

        //Update
        public async Task<object?> GetByIdAsync(long id)
        {
            try
            {
                var result = await _apiAdapter.GetAsync<object>($"VehicleCheckin/{id}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
                return new { success = false, message = "Không thể tải dữ liệu chi tiết", error = ex.Message };
            }
        }

        public async Task<bool> DeleteAsync(long id)
        {
            return await _apiAdapter.DeleteAsync($"VehicleCheckin/{id}");
        }

        public async Task<object?> SearchVehicleAsync(string searchTerm)
        {
            return await _apiAdapter.GetAsync<object>($"VehicleCheckin/search-vehicle?searchTerm={Uri.EscapeDataString(searchTerm)}");
        }

        public async Task<object?> UpdateAsync(long id, Dictionary<string, object> formData)
        {
            try
            {
                // Gọi API backend với dữ liệu thật từ form
                var result = await _apiAdapter.PutAsync<object>($"VehicleCheckin/{id}", formData);
                return new { success = true, data = result, message = "Cập nhật thành công" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateAsync: {ex.Message}");
                return new { success = false, message = ex.Message };
            }
        }

        /// <summary>
        /// Lấy thông tin employee hiện tại từ JWT token
        /// </summary>
        public async Task<object?> GetEmployeeInfoAsync()
        {
            try
            {
                var result = await _apiAdapter.GetAsync<object>("Employee/me");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetEmployeeInfoAsync: {ex.Message}");
                return new { success = false, message = "Không thể lấy thông tin employee", error = ex.Message };
            }
        }

    }
}


