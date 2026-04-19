using System.Collections.Generic;
using System.Threading.Tasks;
using BE.DTOs.Component;

namespace BE.interfaces
{
    public interface IComponentService
    {
        Task<IEnumerable<ResponseDto>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, long? typeComponentId = null, string? statusCode = null, string? search = null);
        Task<int> GetTotalCountAsync(long? branchId = null, long? typeComponentId = null, string? statusCode = null, string? search = null);
        Task<ResponseDto?> GetByIdAsync(long id);
        Task<ResponseDto> CreateAsync(RequestDto dto);
        Task<ResponseDto?> UpdateAsync(RequestDto dto);
        Task DisableEnableAsync(long id, string statusCode);
        Task<int> BatchUpdateStatusAsync(List<long> componentIds, string statusCode);
    }
}