using FE.adapters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FE.services
{
    public class ComponentService
    {
        private readonly ApiAdapter _apiAdapter;

        public ComponentService(ApiAdapter apiAdapter)
        {
            _apiAdapter = apiAdapter;
        }

        public async Task<object?> GetAllAsync(int page = 1, int pageSize = 10, string? search = null, string? statusCode = null, long? typeComponentId = null)
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");

            if (!string.IsNullOrWhiteSpace(statusCode))
                queryParams.Add($"statusCode={Uri.EscapeDataString(statusCode)}");

            // BE sẽ tự động lấy branchId từ JWT token, không cần gửi từ FE
            if (typeComponentId.HasValue)
                queryParams.Add($"typeComponentId={typeComponentId.Value}");

            var queryString = string.Join("&", queryParams);
            return await _apiAdapter.GetAsync<object>($"Component?{queryString}");
        }

        public async Task<object?> GetByIdAsync(long id)
        {
            try
            {
                var result = await _apiAdapter.GetAsync<object>($"Component/{id}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
                return new { success = false, message = "Không thể tải dữ liệu chi tiết", error = ex.Message };
            }
        }

        public async Task<bool> ToggleStatusAsync(long id, string statusCode)
        {
            try
            {
                return await _apiAdapter.PatchAsync($"Component/{id}/status?statusCode={statusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ToggleStatusAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<object?> UpdateAsync(long id, object data)
        {
            try
            {
                var result = await _apiAdapter.PutAsync<object>($"Component/{id}", data);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateAsync: {ex.Message}");
                return new { success = false, message = "Không thể cập nhật", error = ex.Message };
            }
        }
    }
}
