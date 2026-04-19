using AutoMapper;
using BE.DTOs.Feedback;
using BE.interfaces;
using BE.models;
using BE.repository.IRepository;

namespace BE.services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository _repository;
        private readonly IMapper _mapper;

        public FeedbackService(IFeedbackRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ResponseDto>> GetAllAsync()
        {
            var feedbacks = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<ResponseDto>>(feedbacks);
        }

        public async Task<ResponseDto?> GetByIdAsync(long id)
        {
            var feedback = await _repository.GetByIdAsync(id);
            return _mapper.Map<ResponseDto?>(feedback);
        }

        public async Task<IEnumerable<ResponseDto>> GetByUserIdAsync(long userId)
        {
            var feedbacks = await _repository.GetByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<ResponseDto>>(feedbacks);
        }

        public async Task<IEnumerable<ResponseDto>> GetByTicketIdAsync(long ticketId)
        {
            var feedbacks = await _repository.GetByTicketIdAsync(ticketId);
            return _mapper.Map<IEnumerable<ResponseDto>>(feedbacks);
        }

        public async Task<ResponseDto> CreateAsync(RequestDto request)
        {
            var feedback = _mapper.Map<Feedback>(request);
            var created = await _repository.CreateAsync(feedback);
            return _mapper.Map<ResponseDto>(created);
        }

        public async Task<ResponseDto?> UpdateAsync(long id, RequestDto request)
        {
            var feedback = _mapper.Map<Feedback>(request);
            feedback.Id = id;
            var updated = await _repository.UpdateAsync(feedback);
            return _mapper.Map<ResponseDto?>(updated);
        }

        public async Task<bool> DeleteAsync(long id)
        {
            return await _repository.DeleteAsync(id);
        }

        // Mới: filter + paging
        public async Task<(IEnumerable<ResponseDto> Items, int TotalCount)> FilterAsync(int? rating, int page, int pageSize)
        {
            var (items, total) = await _repository.FilterAsync(rating, page, pageSize);
            return (_mapper.Map<IEnumerable<ResponseDto>>(items), total);
        }


        // Mới: lấy reply theo parentId
        public async Task<IEnumerable<ResponseDto>> GetRepliesAsync(long parentId)
        {
            var replies = await _repository.GetRepliesAsync(parentId);
            return _mapper.Map<IEnumerable<ResponseDto>>(replies);
        }

        // Mới: tạo reply (feedback con)
        public async Task<ResponseDto> CreateReplyAsync(RequestDto request)
        {
            if (!request.ParentId.HasValue)
                throw new ArgumentException("ParentId is required for a reply.");

            var feedback = _mapper.Map<Feedback>(request);
            var created = await _repository.CreateAsync(feedback);
            return _mapper.Map<ResponseDto>(created);
        }
    }
}
