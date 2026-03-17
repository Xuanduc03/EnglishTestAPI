using App.Application.Services.Interface;
using App.Application.Vocabularies.Commands;
using App.Application.Vocabularies.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    [ApiController]
    [Route("api/vocabulary")]
    [Authorize]
    public class VocabularyController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public VocabularyController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        // ============================================
        // GET /api/vocabulary/today
        // Lấy danh sách từ cần ôn hôm nay
        // ============================================
        [HttpGet("today")]
        public async Task<IActionResult> GetToday([FromQuery] int maxCards = 20)
        {
            var result = await _mediator.Send(new GetTodayVocabularyQuery
            {
                UserId = _currentUser.UserId!.Value,
                MaxCards = maxCards,
            });
            return Ok(new { success = true, data = result });
        }

        // ============================================
        // POST /api/vocabulary/review
        // Submit kết quả 1 từ (nhớ/quên)
        // ============================================
        [HttpPost("review")]
        public async Task<IActionResult> Review([FromBody] ReviewRequest request)
        {
            var result = await _mediator.Send(new ReviewVocabularyCommand
            {
                UserId = _currentUser.UserId!.Value,
                WordId = request.WordId,
                Remembered = request.Remembered,
            });
            return Ok(new { success = true, data = result });
        }

        // ============================================
        // GET /api/vocabulary/summary
        // Tổng kết phiên học hôm nay
        // ============================================
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] DateTime? date = null)
        {
            var result = await _mediator.Send(new GetVocabSessionSummaryQuery
            {
                UserId = _currentUser.UserId!.Value,
                Date = date,
            });
            return Ok(new { success = true, data = result });
        }

        // ============================================
        // GET /api/vocabulary/words
        // Danh sách từ vựng có phân trang
        // ============================================
        [HttpGet("words")]
        [AllowAnonymous] // Cho phép xem danh sách không cần login
        public async Task<IActionResult> GetWords(
            [FromQuery] string? keyword = null,
            [FromQuery] string? status = null,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetVocabularyWordsQuery
            {
                UserId = _currentUser.UserId,
                Keyword = keyword,
                Status = status,
                PageIndex = pageIndex,
                PageSize = pageSize,
            });
            return Ok(new { success = true, data = result });
        }
    }

    // Request body DTOs
    public class ReviewRequest
    {
        public Guid WordId { get; set; }
        public bool Remembered { get; set; }
    }
}
