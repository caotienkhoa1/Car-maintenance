using FE.adapters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FE.services
{
    public class TypeComponentService
    {
        private readonly ApiAdapter _apiAdapter;

        public TypeComponentService(ApiAdapter apiAdapter)
        {
            _apiAdapter = apiAdapter;
        }

        public async Task<object?> GetAllAsync(int page = 1, int pageSize = 10, string? search = null, string? statusCode = null, long? branchId = null)
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

            // ✅ Thêm branchId vào query nếu có (Admin có thể filter theo chi nhánh)
            if (branchId.HasValue)
                queryParams.Add($"branchId={branchId.Value}");

            var queryString = string.Join("&", queryParams);
            return await _apiAdapter.GetAsync<object>($"TypeComponent?{queryString}");
        }

        public async Task<object?> GetByIdAsync(long id)
        {
            try
            {
                var result = await _apiAdapter.GetAsync<object>($"TypeComponent/{id}");
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
                return await _apiAdapter.PatchAsync($"TypeComponent/{id}/status?statusCode={statusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ToggleStatusAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<object?> CreateAsync(object data)
        {
            try
            {
                var result = await _apiAdapter.PostAsync<object>("TypeComponent", data);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateAsync: {ex.Message}");
                return new { success = false, message = "Không thể tạo loại linh kiện", error = ex.Message };
            }
        }

        public async Task<object?> UpdateAsync(long id, object data)
        {
            try
            {
                var result = await _apiAdapter.PutAsync<object>($"TypeComponent/{id}", data);
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

