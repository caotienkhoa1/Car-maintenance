using FE.adapters;
using FE.  viewmodels;

namespace FE.services
{
    public class AuthService
    {
        private readonly ApiAdapter _apiAdapter;

        public AuthService(ApiAdapter apiAdapter)
        {
            _apiAdapter = apiAdapter;
        }

        public async Task<LoginResponseModel?> LoginAsync(string username, string password)
        {
            try
            {
                Console.WriteLine($"AuthService: Attempting login for user: {username}");
                
                var loginRequest = new LoginRequestModel
                {
                    Username = username,
                    Password = password
                };

                // Call backend API and get wrapped response
                var backendResponse = await _apiAdapter.PostAsync<BackendApiResponse<BackendLoginData>>("Auth/login", loginRequest);
                Console.WriteLine($"AuthService: Backend response - Success: {backendResponse?.Success}");
                Console.WriteLine($"AuthService: Backend response - Message: {backendResponse?.Message}");
                Console.WriteLine($"AuthService: Backend response - Data: {backendResponse?.Data != null}");
                
                if (backendResponse?.Success == true && backendResponse.Data != null)
                {
                    // Map backend response to frontend model
                    var result = new LoginResponseModel
                    {
                        Success = backendResponse.Success,
                        Message = backendResponse.Message,
                        Token = backendResponse.Data.Token,
                        UserId = (int)(backendResponse.Data.UserId ?? 0),
                        Username = backendResponse.Data.Username,
                        Role = backendResponse.Data.RoleName,
                        RoleId = (int)(backendResponse.Data.RoleId ?? 0),
                        BranchId = backendResponse.Data.BranchId,
                        FirstName = backendResponse.Data.FirstName,
                        LastName = backendResponse.Data.LastName
                    };
                    
                    Console.WriteLine($"AuthService: Mapped result - Success: {result.Success}, RoleId: {result.RoleId}");
                    return result;
                }
                
                return new LoginResponseModel { Success = false, Error = backendResponse?.Message ?? "Login failed" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AuthService: Exception during login: {ex.Message}");
                return new LoginResponseModel { Success = false, Error = ex.Message };
            }
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                // Add JWT token to request header
                var token = GetStoredToken();
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                // For now, just clear local storage
                // In a real implementation, you would call the logout API
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string? GetStoredToken()
        {
            // This would typically get the token from a secure storage
            // For now, we'll use a simple approach
            return null;
        }

        public void StoreToken(string token)
        {
            // Store token in secure storage
            // For now, we'll use localStorage via JavaScript
        }

        public void ClearToken()
        {
            // Clear token from storage
        }

        // ✅ Lấy thông tin AutoOwner từ BE API
        public async Task<AutoOwnerInfoResponse?> GetAutoOwnerInfoAsync(long userId)
        {
            try
            {
                // BE API trả về { success: true, data: {...} }
                var response = await _apiAdapter.GetAsync<BackendApiResponse<AutoOwnerInfoResponse>>($"AutoOwner/{userId}");
                if (response?.Success == true && response.Data != null)
                {
                    return response.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting AutoOwner info: {ex.Message}");
                return null;
            }
        }
    }

    // ✅ Model để nhận thông tin AutoOwner từ BE (tương ứng với ResponseDto)
    public class AutoOwnerInfoResponse
    {
        public long Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Username { get; set; }
    }
}
