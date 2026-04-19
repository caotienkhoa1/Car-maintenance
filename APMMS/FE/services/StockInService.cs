using FE.adapters;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FE.services
{
    public class StockInService
    {
        private readonly ApiAdapter _apiAdapter;

        public StockInService(ApiAdapter apiAdapter)
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

            if (branchId.HasValue)
                queryParams.Add($"branchId={branchId.Value}");

            var queryString = string.Join("&", queryParams);
            return await _apiAdapter.GetAsync<object>($"StockIn?{queryString}");
        }

        public async Task<object?> GetByIdAsync(long id)
        {
            try
            {
                var result = await _apiAdapter.GetAsync<object>($"StockIn/{id}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
                return new { success = false, message = "Không thể tải dữ liệu chi tiết", error = ex.Message };
            }
        }

        public async Task<object?> CreateAsync(object data)
        {
            try
            {
                var result = await _apiAdapter.PostAsync<object>("StockIn", data);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateAsync: {ex.Message}");
                return new { success = false, message = "Không thể tạo phiếu nhập kho", error = ex.Message };
            }
        }

        public async Task<object?> UpdateAsync(long id, object data)
        {
            try
            {
                var result = await _apiAdapter.PutAsync<object>($"StockIn/{id}", data);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateAsync: {ex.Message}");
                return new { success = false, message = "Không thể cập nhật", error = ex.Message };
            }
        }

        public async Task<object?> ChangeStatusAsync(long id, string statusCode)
        {
            try
            {
                var success = await _apiAdapter.PatchAsync($"StockIn/{id}/status?statusCode={Uri.EscapeDataString(statusCode)}");
                return new { success = success, message = success ? "Thay đổi trạng thái thành công" : "Không thể thay đổi trạng thái" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ChangeStatusAsync: {ex.Message}");
                return new { success = false, message = "Không thể thay đổi trạng thái", error = ex.Message };
            }
        }

        public async Task<object?> ApproveAsync(long id, object data)
        {
            try
            {
                var result = await _apiAdapter.PostAsync<object>($"StockIn/{id}/approve", data);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ApproveAsync: {ex.Message}");
                return new { success = false, message = "Không thể duyệt phiếu nhập kho", error = ex.Message };
            }
        }

        public async Task<object?> UpdateQuantityAfterCheckAsync(long id, object data)
        {
            try
            {
                var result = await _apiAdapter.PutAsync<object>($"StockIn/{id}/quantity-after-check", data);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateQuantityAfterCheckAsync: {ex.Message}");
                return new { success = false, message = "Không thể cập nhật số lượng sau kiểm tra", error = ex.Message };
            }
        }

        public async Task<object?> CancelAsync(long id)
        {
            try
            {
                var result = await _apiAdapter.PostAsync<object>($"StockIn/{id}/cancel", null);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CancelAsync: {ex.Message}");
                return new { success = false, message = "Không thể hủy phiếu nhập kho", error = ex.Message };
            }
        }

        public async Task<object?> GetByStatusAsync(string statusCode)
        {
            try
            {
                var result = await _apiAdapter.GetAsync<object>($"StockIn/status/{Uri.EscapeDataString(statusCode)}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByStatusAsync: {ex.Message}");
                return new { success = false, message = "Không thể tải dữ liệu", error = ex.Message };
            }
        }

        public async Task<object?> UploadAsync(IFormFile file)
        {
            try
            {
                var result = await _apiAdapter.UploadFileAsync<object>("StockIn/upload", file);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UploadAsync: {ex.Message}");
                return new { success = false, message = "Không thể upload file Excel", error = ex.Message };
            }
        }
    }
}

