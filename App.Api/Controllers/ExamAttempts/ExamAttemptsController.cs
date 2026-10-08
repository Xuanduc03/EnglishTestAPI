using App.Application.ExamAttempts.Commands;
using App.Application.ExamAttempts.Queries;
using App.Application.Practices.Queries;
using App.Application.Services.Interface;
using App.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace App.Api.Controllers.ExamAttempts
{
    /// <summary>
    /// API 
    /// </summary>
    [Route("api/exam-attempts")]
    [ApiController]
    [Authorize]
    public class ExamAttemptsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public ExamAttemptsController(IMediator mediator, ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost("start")]
        [AllowAnonymous]
        [EnableRateLimiting("exam-start")]
        public async Task<IActionResult> StartExamAttempts([FromBody] StartExamCommand command)
        {
            command.UserId = _currentUserService.UserId ?? Guid.Empty;
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }


        /// POST /api/exam-attempts/{attemptId}/submit <summary>
        /// Command : Nộp bài + chấm điểm
        [HttpPost("{attemptId}/submit")]
        [AllowAnonymous]
        public async Task<IActionResult> Submit(Guid attemptId)
        {
            var command = new SubmitExamCommand
            {
                AttemptId = attemptId,
                UserId = _currentUserService.UserId ?? Guid.Empty,
                GuestToken = Request.Headers["X-Guest-Token"].ToString(),
            };
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost("auto-save")]
        [AllowAnonymous]
        public async Task<IActionResult> AutoSave([FromBody] SaveAnswerCommand command)
        {
            command.GuestToken = Request.Headers["X-Guest-Token"].ToString();
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }


        // ============================================
        // GET /api/exam-attempts/history
        // Lịch sử thi của user hiện tại
        // ============================================
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            var query = new GetExamHistoryQuery
            {
                UserId = CurrentUserId,
                PageIndex = pageIndex,
                PageSize = pageSize,
                Status = status,
            };
            var result = await _mediator.Send(query);
            return Ok(new { success = true, data = result });
        }

        // ============================================
        // GET /api/exam-attempts/{attemptId}/review
        // Xem lại chi tiết bài thi
        // ============================================
        [HttpGet("{attemptId:guid}/review")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReview(Guid attemptId)
        {
            var result = await _mediator.Send(new GetExamReviewQuery(
                attemptId, Request.Headers["X-Guest-Token"].ToString()));
            return Ok(new { success = true, data = result });
        }

        /// <summary>
        /// GET /api/exam-attempts/{attemptId}/result
        /// Xem kết quả sau khi nộp bài (điểm TOEIC + thống kê từng Part)
        /// </summary>
        [HttpGet("{attemptId:guid}/result")]
        [AllowAnonymous]
        public async Task<IActionResult> GetResult(Guid attemptId)
        {
            var result = await _mediator.Send(new GetExamResultQuery
            {
                AttemptId = attemptId,
                GuestToken = Request.Headers["X-Guest-Token"].ToString(),
            });
            return Ok(new { success = true, data = result });
        }

        /// <summary>
        /// Phân tích điểm yếu/mạnh theo skill 
        /// </summary>
        /// <param name="lastN"></param>
        /// <returns></returns>
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics([FromQuery] int lastN = 5)
        {
            var result = await _mediator.Send(new GetExamAnalyticsQuery
            {
                UserId = CurrentUserId,
                LastN = lastN,
            });
            return Ok(new { success = true, data = result });
        }

        private Guid CurrentUserId => _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Invalid user token");
    }
}
