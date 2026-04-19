using BE.DTOs.Employee;

namespace BE.interfaces
{
    public interface IEmployeeService
    {
        Task<IEnumerable<EmployeeResponseDto>> GetAllAsync();
        Task<EmployeeResponseDto?> GetByIdAsync(long id);
        Task<EmployeeResponseDto> CreateAsync(EmployeeRequestDto dto, long? createdByUserId = null);
        Task<EmployeeResponseDto?> UpdateAsync(long id, EmployeeRequestDto dto, long? modifiedByUserId = null);
        Task<bool> DeleteAsync(long id);
        Task<IEnumerable<EmployeeResponseDto>> FilterAsync(bool? isDelete, long? roleId, long? branchId = null);
        Task<EmployeeProfileDto?> GetProfileAsync(long userId);
        Task<EmployeeProfileDto?> UpdateProfileAsync(long userId, UpdateProfileDto dto);
        Task<object> GetWithFiltersAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null, long? roleId = null, long? branchId = null);
        Task<EmployeeResponseDto?> UpdateStatusAsync(long id, string statusCode);
    }
}



