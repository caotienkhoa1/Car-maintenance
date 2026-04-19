using BE.DTOs.Employee;
using Microsoft.AspNetCore.Http;

namespace BE.interfaces
{
    public interface IProfileService
    {
        Task<EmployeeResponseDto?> GetMyProfileAsync(long userId);
        Task<EmployeeResponseDto> UpdateMyProfileAsync(long userId, EmployeeProfileUpdateDto dto);
        Task<string> UploadAvatarAsync(long userId, IFormFile file);
    }
}


