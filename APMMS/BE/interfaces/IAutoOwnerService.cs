using BE.DTOs.AutoOwner;

namespace BE.interfaces
{
    public interface IAutoOwnerService
    {
        Task<List<ResponseDto>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<object> GetWithFiltersAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null, long? roleId = null);
        Task<ResponseDto?> GetByIdAsync(long id);
        Task<ResponseDto> CreateAsync(RequestDto dto);
        Task<ResponseDto> UpdateAsync(long id, RequestDto dto);
        Task<int> GetTotalCountAsync(string? search = null, string? status = null, long? roleId = null);
        Task<ResponseDto?> UpdateStatusAsync(long id, string statusCode);
    }
}
