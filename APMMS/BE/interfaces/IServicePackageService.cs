using BE.DTOs.ServicePackage;

namespace BE.interfaces
{
    public interface IServicePackageService
    {
        Task<IEnumerable<ResponseDto>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, string? statusCode = null, string? search = null);
        Task<int> GetTotalCountAsync(long? branchId = null, string? statusCode = null, string? search = null);
        Task<ResponseDto?> GetByIdAsync(long id);
        Task<ResponseDto> CreateAsync(RequestDto dto);
        Task<ResponseDto?> UpdateAsync(RequestDto dto);
        Task DisableEnableAsync(long id, string statusCode);
    }
}


