using BE.DTOs.ServiceSchedule;

namespace BE.interfaces
{
    public interface IServiceScheduleService
    {
        Task<ResponseDto> CreateScheduleAsync(RequestDto request, long? currentUserId = null);
        Task<ResponseDto> GetScheduleByIdAsync(long id);
        Task<List<ListResponseDto>> GetAllSchedulesAsync(int page = 1, int pageSize = 10);
        Task<List<ListResponseDto>> GetSchedulesByUserIdAsync(long userId);
        Task<List<ListResponseDto>> GetSchedulesByBranchIdAsync(long branchId);
        Task<List<ListResponseDto>> GetSchedulesByStatusAsync(string statusCode, long? branchId = null);
        Task<List<ListResponseDto>> GetSchedulesByDateRangeAsync(DateTime startDate, DateTime endDate, long? branchId = null);
        Task<List<ListResponseDto>> GetTodaySchedulesAsync(long? branchId = null);
        Task<int> GetTodaySchedulesCountAsync(long? branchId = null);
        Task<ResponseDto> UpdateScheduleAsync(long id, UpdateScheduleDto request, long? currentUserId = null);
        Task<ResponseDto> CancelScheduleAsync(long id, CancelScheduleDto? request = null, long? currentUserId = null);
        Task<ResponseDto> CompleteScheduleAsync(long id, long? currentUserId = null);
        Task<bool> DeleteScheduleAsync(long id);
        Task<ResponseDto> CreatePublicBookingAsync(PublicBookingDto request);
        Task<ResponseDto> AcceptScheduleAsync(long id, AcceptScheduleDto request, long? currentUserId = null);
        Task<NoteResponseDto> AddNoteAsync(long scheduleId, AddNoteDto request, long? currentUserId = null);
        Task<List<NoteResponseDto>> GetNotesAsync(long scheduleId);
    }
}
