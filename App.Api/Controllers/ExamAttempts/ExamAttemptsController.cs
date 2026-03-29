using App.Application.ExamAttempts.Commands;
using App.Application.ExamAttempts.Queries;
using App.Application.Practices.Queries;
using App.Application.Services.Interface;
using App.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers.ExamAttempts
{
    /// <summary>
    /// API 
    /// </summary>
    [Route("api/exam-attempts")]
    [ApiController]
    public class ExampAttemptController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public ExampAttemptController(IMediator mediator, ICurrentUserService currentUserService)
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
        public async Task<IActionResult> StartExamAttempts([FromBody] StartExamCommand command)
        {
            command.UserId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User not found");
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }


        /// POST /api/exam-attempts/{attemptId}/submit <summary>
        /// Command : Nộp bài + chấm điểm
        [HttpPost("{attemptId}/submit")]
        public async Task<IActionResult> Submit(Guid attemptId)
        {
            var UserId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User not found");
            var command = new SubmitExamCommand
            {
                AttemptId = attemptId,
                UserId = UserId,
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
        public async Task<IActionResult> AutoSave([FromBody] SaveAnswerCommand command)
        {
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
                UserId = _currentUserService.UserId!.Value,
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
        public async Task<IActionResult> GetReview(Guid attemptId)
        {
            var result = await _mediator.Send(new GetExamReviewQuery(attemptId));
            return Ok(new { success = true, data = result });
        }

        /// <summary>
        /// GET /api/exam-attempts/{attemptId}/result
        /// Xem kết quả sau khi nộp bài (điểm TOEIC + thống kê từng Part)
        /// </summary>
        [HttpGet("{attemptId:guid}/result")]
        public async Task<IActionResult> GetResult(Guid attemptId)
        {
            var result = await _mediator.Send(new GetExamResultQuery
            {
                AttemptId = attemptId,
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
                UserId = _currentUserService.UserId!.Value,
                LastN = lastN,
            });
            return Ok(new { success = true, data = result });
        }
    }
}
