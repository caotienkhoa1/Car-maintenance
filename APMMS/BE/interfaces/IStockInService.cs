using System.Collections.Generic;
using System.Threading.Tasks;
using BE.DTOs.StockIn;
using Microsoft.AspNetCore.Http;

namespace BE.interfaces
{
    public interface IStockInService
    {
        Task<StockInUploadResponseDto> UploadExcelAsync(IFormFile file);
        Task<StockInResponseDto> CreateFromRequestAsync(StockInRequestDto dto, long userId);
        Task<StockInResponseDto> UpdateAsync(StockInRequestDto dto, long userId);
        Task<StockInResponseDto> GetByIdAsync(long id);
        Task<IEnumerable<StockInResponseDto>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, string? statusCode = null, string? search = null);
        Task<int> GetTotalCountAsync(long? branchId = null, string? statusCode = null, string? search = null);
        Task<IEnumerable<StockInResponseDto>> GetByStatusAsync(string statusCode);
        Task<StockInResponseDto> ChangeStatusAsync(long id, string statusCode, long userId);
        Task<StockInResponseDto> ApproveAsync(StockInRequestDto dto, long userId);
        Task<StockInResponseDto> UpdateQuantityAfterCheckAsync(StockInRequestDto dto, long userId);
        Task<StockInResponseDto> CancelAsync(long id, long userId);
        Task<bool> ExistsAsync(long id);
    }
}

