using BE.DTOs.ServiceTask;

namespace BE.interfaces
{
    public interface IServiceTaskService
    {
        Task<ServiceTaskResponseDto> CreateServiceTaskAsync(ServiceTaskRequestDto request, long? userId = null);
        Task<ServiceTaskResponseDto> UpdateServiceTaskAsync(ServiceTaskUpdateDto request);
        Task<ServiceTaskResponseDto> GetServiceTaskByIdAsync(long id);
        Task<List<ServiceTaskListResponseDto>> GetAllServiceTasksAsync(int page = 1, int pageSize = 10);
        Task<List<ServiceTaskListResponseDto>> GetServiceTasksByMaintenanceTicketIdAsync(long maintenanceTicketId);
        Task<List<ServiceTaskListResponseDto>> GetServiceTasksByStatusAsync(string statusCode);
        Task<List<ServiceTaskListResponseDto>> GetServiceTasksByTechnicianIdAsync(long technicianId);
        Task<bool> DeleteServiceTaskAsync(long id);
        Task<ServiceTaskResponseDto> UpdateStatusAsync(long id, string statusCode, long? userId = null, string? completionNote = null);
        Task<ServiceTaskResponseDto> UpdateLaborTimeAsync(long id, decimal actualLaborTime);
        Task<ServiceTaskResponseDto> AssignTechniciansAsync(long id, ServiceTaskAssignTechniciansDto request, long? userId = null);
    }
}


