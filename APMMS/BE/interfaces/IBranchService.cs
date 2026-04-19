using BE.DTOs.Branch;
using BE.DTOs.Employee;

namespace BE.interfaces
{
    public interface IBranchService
    {
        Task<IEnumerable<BranchResponseDto>> GetAllAsync();
        Task<BranchResponseDto?> GetByIdAsync(long id);
        Task<BranchResponseDto> CreateAsync(BranchRequestDto dto);
        Task<BranchResponseDto?> UpdateAsync(long id, BranchRequestDto dto);
        Task<bool> DeleteAsync(long id);
        Task<EmployeeResponseDto?> GetDirectorAsync(long branchId);
        Task<bool> ChangeDirectorAsync(long branchId, long newDirectorId);
        Task<EmployeeResponseDto> CreateDirectorAsync(long branchId, CreateDirectorDto dto, long? createdByUserId = null);
    }
}

