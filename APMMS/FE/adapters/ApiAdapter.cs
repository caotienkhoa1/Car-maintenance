using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace FE.adapters
{
    public class ApiAdapter
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiAdapter(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7000/api";
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                var url = $"{_baseUrl}/{endpoint}";
                Console.WriteLine($"ApiAdapter: Calling GET {url}");
                
                // ✅ Thêm Authorization header từ HttpContext session hoặc cookie
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var httpContext = _httpContextAccessor.HttpContext;
                string? token = null;
                
                if (httpContext != null)
                {
                    // Thử lấy token từ session trước (được lưu khi login)
                    token = httpContext.Session.GetString("AuthToken");
                    if (string.IsNullOrEmpty(token))
                    {
                        // Thử lấy từ cookie
                        token = httpContext.Request.Cookies["authToken"];
                    }
                    if (string.IsNullOrEmpty(token))
                    {
                        // Thử lấy từ header
                        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                        {
                            token = authHeader.Substring(7); // Remove "Bearer " prefix
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        Console.WriteLine($"ApiAdapter: Added Authorization header with token (length: {token.Length})");
                    }
                    else
                    {
                        Console.WriteLine("ApiAdapter: No token found in session, cookie, or header");
                    }
                }
                
                var response = await _httpClient.SendAsync(request);
                Console.WriteLine($"ApiAdapter: Response status: {response.StatusCode}");
                
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"ApiAdapter: Response content: {content}");
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"ApiAdapter: Request failed with status {response.StatusCode}");
                    throw new HttpRequestException($"API request failed with status {response.StatusCode}: {content}");
                }
                
                return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ApiAdapter: Exception in GetAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<T?> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var url = $"{_baseUrl}/{endpoint}";
                Console.WriteLine($"ApiAdapter: Calling POST {url}");
                Console.WriteLine($"ApiAdapter: Request data: {json}");
                
                // ✅ Thêm Authorization header từ HttpContext session hoặc cookie
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                
                var httpContext = _httpContextAccessor.HttpContext;
                string? token = null;
                
                if (httpContext != null)
                {
                    // Thử lấy token từ session trước (được lưu khi login)
                    token = httpContext.Session.GetString("AuthToken");
                    if (string.IsNullOrEmpty(token))
                    {
                        // Thử lấy từ cookie
                        token = httpContext.Request.Cookies["authToken"];
                    }
                    if (string.IsNullOrEmpty(token))
                    {
                        // Thử lấy từ header
                        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                        {
                            token = authHeader.Substring(7); // Remove "Bearer " prefix
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        Console.WriteLine($"ApiAdapter: Added Authorization header with token (length: {token.Length})");
                    }
                    else
                    {
                        Console.WriteLine("ApiAdapter: No token found in session, cookie, or header");
                    }
                }
                
                var response = await _httpClient.SendAsync(request);
                Console.WriteLine($"ApiAdapter: Response status: {response.StatusCode}");
                
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"ApiAdapter: Response content: {responseContent}");
                
                // ✅ Always try to deserialize, even for error responses
                try
                {
                    var result = JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Console.WriteLine($"ApiAdapter: Deserialized result: {result}");
                    return result;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"ApiAdapter: JSON deserialization error: {ex.Message}");
                    Console.WriteLine($"ApiAdapter: Raw response: {responseContent}");
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ApiAdapter: Exception: {ex.Message}");
                return default(T);
            }
        }

        public async Task<bool> PutAsync(string endpoint, object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"{_baseUrl}/{endpoint}", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Log error
                return false;
            }
        }

        public async Task<T?> PutAsync<T>(string endpoint, object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var url = $"{_baseUrl}/{endpoint}";
                Console.WriteLine($"ApiAdapter: Calling PUT {url}");
                Console.WriteLine($"ApiAdapter: Request data: {json}");
                
                // ✅ Thêm Authorization header từ HttpContext session hoặc cookie
                var request = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = content
                };
                
                var httpContext = _httpContextAccessor.HttpContext;
                string? token = null;
                
                if (httpContext != null)
                {
                    // Thử lấy token từ session trước (được lưu khi login)
                    token = httpContext.Session.GetString("AuthToken");
                    if (string.IsNullOrEmpty(token))
                    {
                        // Thử lấy từ cookie
                        token = httpContext.Request.Cookies["authToken"];
                    }
                    if (string.IsNullOrEmpty(token))
                    {
                        // Thử lấy từ header
                        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                        {
                            token = authHeader.Substring(7); // Remove "Bearer " prefix
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        Console.WriteLine($"ApiAdapter: Added Authorization header with token (length: {token.Length})");
                    }
                    else
                    {
                        Console.WriteLine("ApiAdapter: No token found in session, cookie, or header");
                    }
                }
                
                var response = await _httpClient.SendAsync(request);
                Console.WriteLine($"ApiAdapter: Response status: {response.StatusCode}");
                
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"ApiAdapter: Response content: {responseContent}");
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"ApiAdapter: Request failed with status {response.StatusCode}");
                    throw new HttpRequestException($"API request failed with status {response.StatusCode}: {responseContent}");
                }
                
                return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ApiAdapter: Exception in PutAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> PatchAsync(string endpoint, object? data = null)
        {
            try
            {
                var url = $"{_baseUrl}/{endpoint}";
                Console.WriteLine($"ApiAdapter: Calling PATCH {url}");
                
                HttpContent? content = null;
                if (data != null)
                {
                    var json = JsonSerializer.Serialize(data);
                    content = new StringContent(json, Encoding.UTF8, "application/json");
                    Console.WriteLine($"ApiAdapter: Request data: {json}");
                }
                else
                {
                    content = new StringContent("{}", Encoding.UTF8, "application/json");
                }
                
                var request = new HttpRequestMessage(HttpMethod.Patch, url)
                {
                    Content = content
                };
                
                // Add authorization if available
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    var token = httpContext.Session.GetString("AuthToken") 
                        ?? httpContext.Request.Cookies["authToken"];
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                }
                
                var response = await _httpClient.SendAsync(request);
                Console.WriteLine($"ApiAdapter: Response status: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ApiAdapter: Exception in PatchAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/{endpoint}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Log error
                return false;
            }
        }

        public async Task<T?> UploadFileAsync<T>(string endpoint, IFormFile file)
        {
            try
            {
                var url = $"{_baseUrl}/{endpoint}";
                Console.WriteLine($"ApiAdapter: Calling POST {url} (file upload)");

                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(streamContent, "file", file.FileName);

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                // Add authorization
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    var token = httpContext.Session.GetString("AuthToken")
                        ?? httpContext.Request.Cookies["authToken"];
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        Console.WriteLine($"ApiAdapter: Added Authorization header for file upload");
                    }
                }

                var response = await _httpClient.SendAsync(request);
                Console.WriteLine($"ApiAdapter: Upload response status: {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"ApiAdapter: Upload response content: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Upload failed with status {response.StatusCode}: {responseContent}");
                }

                return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ApiAdapter: Exception in UploadFileAsync: {ex.Message}");
                throw;
            }
        }
    }
}
