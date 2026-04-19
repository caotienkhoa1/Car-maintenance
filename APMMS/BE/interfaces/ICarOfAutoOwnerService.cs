using BE.DTOs.CarOfAutoOwner;

namespace BE.interfaces
{
    public interface ICarOfAutoOwnerService
    {
        Task<List<ResponseDto>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<ResponseDto?> GetByIdAsync(long id);
        Task<List<ResponseDto>> GetByUserIdAsync(long userId);
        Task<List<ResponseDto>> GetServicedCarsByUserIdAsync(long userId);
        Task<DuplicateCheckResponseDto> CheckDuplicateAsync(string? licensePlate = null, string? vinNumber = null, string? engineNumber = null, long? excludeCarId = null);
        Task<ResponseDto> CreateAsync(RequestDto dto);
        Task<ResponseDto> UpdateAsync(long id, RequestDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
