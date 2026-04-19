using FE.adapters;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FE.services
{
    public class StockInRequestService
    {
        private readonly ApiAdapter _apiAdapter;
        private readonly HttpClient _httpClient;

        public StockInRequestService(ApiAdapter apiAdapter, IHttpClientFactory httpClientFactory)
        {
            _apiAdapter = apiAdapter;
            _httpClient = httpClientFactory.CreateClient("ApiClient");
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
            return await _apiAdapter.GetAsync<object>($"StockInRequest?{queryString}");
        }

        public async Task<object?> GetByIdAsync(long id)
        {
            try
            {
                var result = await _apiAdapter.GetAsync<object>($"StockInRequest/{id}");
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
                var result = await _apiAdapter.PostAsync<object>("StockInRequest", data);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateAsync: {ex.Message}");
                return new { success = false, message = "Không thể tạo yêu cầu nhập kho", error = ex.Message };
            }
        }

        public async Task<object?> UpdateAsync(long id, object data)
        {
            try
            {
                var result = await _apiAdapter.PutAsync<object>($"StockInRequest/{id}", data);
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
                var success = await _apiAdapter.PatchAsync($"StockInRequest/{id}/status?statusCode={Uri.EscapeDataString(statusCode)}");
                return new { success = success, message = success ? "Thay đổi trạng thái thành công" : "Không thể thay đổi trạng thái" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ChangeStatusAsync: {ex.Message}");
                return new { success = false, message = "Không thể thay đổi trạng thái", error = ex.Message };
            }
        }

        public async Task<object?> ApproveAsync(long id)
        {
            try
            {
                var result = await _apiAdapter.PostAsync<object>($"StockInRequest/{id}/approve", null);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ApproveAsync: {ex.Message}");
                return new { success = false, message = "Không thể duyệt yêu cầu", error = ex.Message };
            }
        }

        public async Task<object?> SendAsync(long id)
        {
            try
            {
                var result = await _apiAdapter.PostAsync<object>($"StockInRequest/{id}/send", null);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendAsync: {ex.Message}");
                return new { success = false, message = "Không thể gửi yêu cầu", error = ex.Message };
            }
        }

        public async Task<object?> CancelAsync(long id, string? note = null)
        {
            try
            {
                var data = note != null ? new { note } : null;
                var result = await _apiAdapter.PostAsync<object>($"StockInRequest/{id}/cancel", data);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CancelAsync: {ex.Message}");
                return new { success = false, message = "Không thể hủy yêu cầu", error = ex.Message };
            }
        }

        public async Task<object?> GetByStatusAsync(string statusCode)
        {
            try
            {
                var result = await _apiAdapter.GetAsync<object>($"StockInRequest/status/{Uri.EscapeDataString(statusCode)}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByStatusAsync: {ex.Message}");
                return new { success = false, message = "Không thể tải dữ liệu", error = ex.Message };
            }
        }

        public async Task<(bool Success, byte[]? Data, string? ContentType, string? FileName, string? Message)> DownloadTemplateAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/StockInRequest/template");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsByteArrayAsync();
                    var contentType = response.Content.Headers.ContentType?.ToString();
                    var fileName = response.Content.Headers.ContentDisposition?.FileNameStar ??
                                   response.Content.Headers.ContentDisposition?.FileName;
                    return (true, content, contentType, fileName, null);
                }
                var errorMsg = await response.Content.ReadAsStringAsync();
                return (false, null, null, null, errorMsg);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DownloadTemplateAsync: {ex.Message}");
                return (false, null, null, null, ex.Message);
            }
        }

        public async Task<object?> UploadExcelAsync(IFormFile file)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "file", file.FileName);

                var response = await _httpClient.PostAsync("api/StockInRequest/upload", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    return System.Text.Json.JsonSerializer.Deserialize<object>(responseContent, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                
                return new { success = false, message = responseContent };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UploadExcelAsync: {ex.Message}");
                return new { success = false, message = "Không thể upload file Excel", error = ex.Message };
            }
        }
    }
}

