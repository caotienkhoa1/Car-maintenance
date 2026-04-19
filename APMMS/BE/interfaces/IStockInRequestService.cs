using System.Collections.Generic;
using System.Threading.Tasks;
using BE.DTOs.StockInRequest;
using Microsoft.AspNetCore.Http;

namespace BE.interfaces
{
    public interface IStockInRequestService
    {
        Task<StockInRequestUploadResponseDto> UploadExcelAsync(IFormFile file);
        Task<StockInRequestResponseDto> CreateAsync(StockInRequestRequestDto dto, long userId);
        Task<StockInRequestResponseDto> UpdateAsync(StockInRequestRequestDto dto, long userId);
        Task<StockInRequestResponseDto> GetByIdAsync(long id);
        Task<IEnumerable<StockInRequestResponseDto>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, string? statusCode = null, string? search = null);
        Task<int> GetTotalCountAsync(long? branchId = null, string? statusCode = null, string? search = null);
        Task<IEnumerable<StockInRequestResponseDto>> GetByStatusAsync(string statusCode);
        Task<StockInRequestResponseDto> ChangeStatusAsync(long id, string statusCode, long userId);
        Task<StockInRequestResponseDto> ApproveAsync(long id, long userId);
        Task<StockInRequestResponseDto> CancelAsync(long id, string? note, long userId);
        Task<bool> ExistsAsync(long id);
    }
}

