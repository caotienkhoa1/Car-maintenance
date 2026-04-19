using BE.models;
using BE.repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace BE.repository
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly CarMaintenanceDbContext _context;

        public FeedbackRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Feedback>> GetAllAsync()
        {
            return await _context.Feedbacks
                .Include(f => f.User)
                .Include(f => f.MaintenanceTicket)
                .ToListAsync();
        }

        public async Task<Feedback?> GetByIdAsync(long id)
        {
            return await _context.Feedbacks
                .Include(f => f.User)
                .Include(f => f.MaintenanceTicket)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<IEnumerable<Feedback>> GetByUserIdAsync(long userId)
        {
            return await _context.Feedbacks
                .Include(f => f.User)
                .Include(f => f.MaintenanceTicket)
                .Where(f => f.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Feedback>> GetByTicketIdAsync(long ticketId)
        {
            return await _context.Feedbacks
                .Include(f => f.User)
                .Include(f => f.MaintenanceTicket)
                .Where(f => f.MaintenanceTicketId == ticketId)
                .ToListAsync();
        }

        public async Task<Feedback> CreateAsync(Feedback feedback)
        {
            feedback.CreatedAt = DateTime.UtcNow;
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            return feedback;
        }

        public async Task<Feedback?> UpdateAsync(Feedback feedback)
        {
            var existing = await _context.Feedbacks.FindAsync(feedback.Id);
            if (existing == null) return null;

            _context.Entry(existing).CurrentValues.SetValues(feedback);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null) return false;

            _context.Feedbacks.Remove(feedback);
            await _context.SaveChangesAsync();
            return true;
        }

        // Mới: filter + paging
        public async Task<(IEnumerable<Feedback> Items, int TotalCount)> FilterAsync(int? rating, int page, int pageSize)
        {
            var query = _context.Feedbacks
                .Include(f => f.User)
                .Include(f => f.MaintenanceTicket)
                .AsQueryable();

            if (rating.HasValue)
                query = query.Where(f => f.Rating == rating.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // Mới: get reply theo parentId
        public async Task<IEnumerable<Feedback>> GetRepliesAsync(long parentId)
        {
            return await _context.Feedbacks
                .Include(f => f.User)
                .Where(f => f.ParentId == parentId)
                .OrderBy(f => f.CreatedAt)
                .ToListAsync();
        }
    }
}
