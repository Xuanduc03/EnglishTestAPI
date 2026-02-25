using App.Application.DTOs;
using App.Application.Practice.Commands;
using App.Application.Practices.Commands;
using App.Application.Practices.Queries;
using App.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace App.Api.Controllers
{
    [ApiController]
    [Route("api/practice")]
    public class PracticeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PracticeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // ============================================
        // 1. START PRACTICE SESSION
        // ============================================

        /// <summary>
        /// Tạo session practice mới
        /// POST /api/practice/start
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartPractice([FromBody] CreatePracticeRequest request)
        {
            var command = new StartPracticeCommand(
                UserId: UserId,
                CategoryIds: request.PartIds,
                QuestionsPerPart: request.QuestionsPerPart,
                IsTimed: request.IsTimed,
                TimeLimitMinutes: request.TimeLimitMinutes
            );

            var result = await _mediator.Send(command);

            return Ok(new
            {
                success = true,
                data = result,
                message = "Practice session started successfully"
            });
        }


        // ============================================
        // 3. SUBMIT PRACTICE SESSION
        // ============================================

        /// <summary>
        /// Submit toàn bộ practice session
        /// POST /api/practice/{sessionId}/submit
        /// </summary>
        [HttpPost("{sessionId}/submit")]
        public async Task<IActionResult> SubmitPractice( Guid sessionId, [FromBody] SubmitPracticeCommand request)
        {
            var command = new SubmitPracticeCommand(
                sessionId,
                request.Answers,
                request.TotalTimeSeconds
            );
            var result = await _mediator.Send(command);
            return Ok(new
            {
                success = true,
                data = result,
                message = "Practice submitted successfully"
            });
        }

        // ============================================
        // 4. GET PRACTICE RESULT
        // ============================================

        /// <summary>
        /// Lấy kết quả practice
        /// GET /api/practice/{sessionId}/result
        /// </summary>
        [HttpGet("{sessionId}/result")]
        public async Task<IActionResult> GetResult(Guid sessionId)
        {
            var query = new GetPracticeResultQuery(sessionId);
            var result = await _mediator.Send(query);

            return Ok(new
            {
                success = true,
                data = result
            });
        }

        // ============================================
        // 5. GET PRACTICE HISTORY
        // ============================================

        /// <summary>
        /// Lấy lịch sử practice
        /// GET /api/practice/history?categoryId=xxx&page=1&pageSize=10
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] Guid? categoryId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetPracticeHistoryQuery(
                UserId: UserId,
                CategoryId: categoryId,
                PageIndex: page,
                PageSize: pageSize
            );

            var result = await _mediator.Send(query);

            return Ok(new
            {
                success = true,
                data = result.Items,
                pagination = new
                {
                    total = result.TotalCount,
                    page = result.PageIndex,
                    pageSize = result.PageSize,
                    totalPages = result.TotalPages
                }
            });
        }

        //// ============================================
        //// 6. GET PRACTICE STATISTICS
        //// ============================================

        ///// <summary>
        ///// Lấy thống kê practice của user
        ///// GET /api/practice/statistics
        ///// </summary>
        //[HttpGet("statistics")]
        //public async Task<IActionResult> GetStatistics()
        //{
        //    var query = new GetPracticeStatisticsQuery(UserId);
        //    var result = await _mediator.Send(query);

        //    return Ok(new
        //    {
        //        success = true,
        //        data = result
        //    });
        //}

        //// ============================================
        //// 7. ABANDON PRACTICE
        //// ============================================

        ///// <summary>
        ///// Bỏ practice giữa chừng
        ///// POST /api/practice/{sessionId}/abandon
        ///// </summary>
        //[HttpPost("{sessionId}/abandon")]
        //public async Task<IActionResult> AbandonPractice(Guid sessionId)
        //{
        //    var command = new AbandonPracticeCommand(sessionId);
        //    await _mediator.Send(command);

        //    return Ok(new
        //    {
        //        success = true,
        //        message = "Practice abandoned"
        //    });
        //}

        //// ============================================
        //// 8. RESUME PRACTICE
        //// ============================================

        /// <summary>
        /// Resume practice session đang dở
        /// GET /api/practice/{sessionId}/resume
        /// </summary>
        [HttpGet("{sessionId}/resume")]
        public async Task<IActionResult> ResumePractice(Guid sessionId)
        {
            var query = new GetPracticeSessionQuery(sessionId);
            var result = await _mediator.Send(query);

            return Ok(new
            {
                success = true,
                data = result,
                message = "Practice session resumed"
            });
        }

        /// <summary>
        /// Hiển thị list các practice đang làm dở 
        /// GET /api/practice/{sessionId}/resume
        /// </summary>
        [HttpGet("in-progress")]
        public async Task<IActionResult> GetInProgressPractices()
        {
            var query = new GetInProgressPracticesQuery();
            var result = await _mediator.Send(query);
            return Ok(new { success = true, data = result });
        }

    }
}
