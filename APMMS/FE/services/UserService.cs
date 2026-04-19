using FE.adapters;
using FE.viewmodels;

namespace FE.services
{
    public class UserService
    {
        private readonly ApiAdapter _apiAdapter;

        public UserService(ApiAdapter apiAdapter)
        {
            _apiAdapter = apiAdapter;
        }

        public async Task<UserModel?> GetUserAsync(long id)
        {
            return await _apiAdapter.GetAsync<UserModel>($"user/{id}");
        }

        public async Task<List<UserModel>?> GetUsersAsync()
        {
            return await _apiAdapter.GetAsync<List<UserModel>>("user");
        }

        public async Task<UserModel?> CreateUserAsync(UserModel user)
        {
            return await _apiAdapter.PostAsync<UserModel>("user", user);
        }

        public async Task<bool> UpdateUserAsync(long id, UserModel user)
        {
            return await _apiAdapter.PutAsync($"user/{id}", user);
        }

        public async Task<bool> DeleteUserAsync(long id)
        {
            return await _apiAdapter.DeleteAsync($"user/{id}");
        }
    }
}
