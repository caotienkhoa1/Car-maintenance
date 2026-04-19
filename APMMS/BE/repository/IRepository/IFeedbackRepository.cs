using BE.models;

namespace BE.repository.IRepository
{
    public interface IFeedbackRepository
    {
        Task<IEnumerable<Feedback>> GetAllAsync();
        Task<Feedback?> GetByIdAsync(long id);
        Task<IEnumerable<Feedback>> GetByUserIdAsync(long userId);
        Task<IEnumerable<Feedback>> GetByTicketIdAsync(long ticketId);
        Task<Feedback> CreateAsync(Feedback feedback);
        Task<Feedback?> UpdateAsync(Feedback feedback);
        Task<bool> DeleteAsync(long id);

        // Mới: Filter theo rating + phân trang
        Task<(IEnumerable<Feedback> Items, int TotalCount)> FilterAsync(int? rating, int page, int pageSize);

        // Mới: Lấy tất cả reply theo parentId
        Task<IEnumerable<Feedback>> GetRepliesAsync(long parentId);
    }
}
