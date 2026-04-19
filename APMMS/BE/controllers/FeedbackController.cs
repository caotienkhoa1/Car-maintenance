using AutoMapper;
using BE.DTOs.Feedback;
using BE.interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BE.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;
        private readonly IMapper _mapper;

        public FeedbackController(IFeedbackService feedbackService, IMapper mapper)
        {
            _feedbackService = feedbackService;
            _mapper = mapper;
        }

        // Lấy tất cả feedback, có filter rating và phân trang
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? rating, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (items, totalCount) = await _feedbackService.FilterAsync(rating, page, pageSize);
            var result = new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = items
            };
            return Ok(result);
        }

        // Lấy feedback theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var feedback = await _feedbackService.GetByIdAsync(id);
            if (feedback == null) return NotFound();
            return Ok(feedback);
        }

        // Lấy tất cả feedback của user
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(long userId)
        {
            var feedbacks = await _feedbackService.GetByUserIdAsync(userId);
            return Ok(feedbacks);
        }

        // Lấy tất cả feedback của ticket
        [HttpGet("ticket/{ticketId}")]
        public async Task<IActionResult> GetByTicketId(long ticketId)
        {
            var feedbacks = await _feedbackService.GetByTicketIdAsync(ticketId);
            return Ok(feedbacks);
        }

        // Lấy tất cả reply của 1 feedback
        [HttpGet("{parentId}/replies")]
        public async Task<IActionResult> GetReplies(long parentId)
        {
            var replies = await _feedbackService.GetRepliesAsync(parentId);
            return Ok(replies);
        }

        // Tạo feedback hoặc reply
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RequestDto request)
        {
            if (request.ParentId.HasValue)
            {
                // Đây là reply
                var reply = await _feedbackService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = reply.Id }, reply);
            }
            else
            {
                // Đây là feedback bình thường
                var feedback = await _feedbackService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = feedback.Id }, feedback);
            }
        }

        // Cập nhật feedback
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] RequestDto request)
        {
            var updated = await _feedbackService.UpdateAsync(id, request);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // Xóa feedback
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _feedbackService.DeleteAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
    }
}
