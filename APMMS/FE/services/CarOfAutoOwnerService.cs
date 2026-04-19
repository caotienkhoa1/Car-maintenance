using FE.adapters;
using FE.viewmodels;

namespace FE.services
{
    public class CarOfAutoOwnerService
    {
        private readonly ApiAdapter _apiAdapter;

        public CarOfAutoOwnerService(ApiAdapter apiAdapter)
        {
            _apiAdapter = apiAdapter;
        }

        public async Task<List<CarOfAutoOwnerViewModel>?> GetCarsByOwnerIdAsync(int ownerId)
            => await _apiAdapter.GetAsync<List<CarOfAutoOwnerViewModel>>($"CarOfAutoOwner/user/{ownerId}");

        public async Task<CarOfAutoOwnerViewModel?> GetCarByIdAsync(int id)
            => await _apiAdapter.GetAsync<CarOfAutoOwnerViewModel>($"CarOfAutoOwner/{id}");

        public async Task<CarOfAutoOwnerViewModel?> CreateCarAsync(CarOfAutoOwnerViewModel car)
            => await _apiAdapter.PostAsync<CarOfAutoOwnerViewModel>("CarOfAutoOwner", car);

        public async Task<CarOfAutoOwnerViewModel?> UpdateCarAsync(int id, CarOfAutoOwnerViewModel car)
            => await _apiAdapter.PutAsync<CarOfAutoOwnerViewModel>($"CarOfAutoOwner/{id}", car);

        public async Task<bool> DeleteCarAsync(int id)
            => await _apiAdapter.DeleteAsync($"CarOfAutoOwner/{id}");
    }
}
