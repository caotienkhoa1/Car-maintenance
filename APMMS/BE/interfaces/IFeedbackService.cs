using BE.DTOs.Feedback;

namespace BE.interfaces
{
    public interface IFeedbackService
    {
        Task<IEnumerable<ResponseDto>> GetAllAsync();
        Task<ResponseDto?> GetByIdAsync(long id);
        Task<IEnumerable<ResponseDto>> GetByUserIdAsync(long userId);
        Task<IEnumerable<ResponseDto>> GetByTicketIdAsync(long ticketId);
        Task<ResponseDto> CreateAsync(RequestDto request);
        Task<ResponseDto?> UpdateAsync(long id, RequestDto request);
        Task<bool> DeleteAsync(long id);

        // Mới
        Task<(IEnumerable<ResponseDto> Items, int TotalCount)> FilterAsync(int? rating, int page, int pageSize);
        Task<IEnumerable<ResponseDto>> GetRepliesAsync(long parentId);
        Task<ResponseDto> CreateReplyAsync(RequestDto request);
    }
}
