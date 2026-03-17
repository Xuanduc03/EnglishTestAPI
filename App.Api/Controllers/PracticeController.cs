using App.Application.DTOs;
using App.Application.Practice.Commands;
using App.Application.Practices.Commands;
using App.Application.Practices.Commands.Writing;
using App.Application.Practices.Queries;
using App.Application.Writing.Queries;
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
        public async Task<IActionResult> SubmitPractice( Guid sessionId, [FromBody] SubmitPracticeRequest request)
        {
            var command = new SubmitPracticeCommand(
                sessionId,
                request.Answers,
                request.TotalTimeSeconds
            );
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
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
        [HttpPost("{sessionId}/abandon")]
        public async Task<IActionResult> AbandonPractice(Guid sessionId, [FromBody] AbandonPracticeCommand request)
        {
            if (sessionId != request.SessionId)
                return BadRequest("Session ID mismatch");

            var result = await _mediator.Send(request);
            return Ok(new { success = result, message = "Practice abandoned" });
        }

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
            var query = new GetPracticeSessionQuery(sessionId, UserId);
            var result = await _mediator.Send(query);
            return Ok(new { success = true, data = result, message = "Practice session resumed" });
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

        /// <summary>
        ///  GET /api/practice/{sessionId}/review trả về danh sách các câu hỏi kèm thông tin chi tiết.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        [HttpGet("{sessionId}/review")]
        public async Task<IActionResult> GetReview(Guid sessionId)
        {
            var query = new GetPracticeReviewQuery(sessionId);
            var result = await _mediator.Send(query);
            return Ok(new { success = true, data = result });
        }



        // ── Start ─────────────────────────────────────────────────────

        /// <summary>
        /// Tạo Writing session mới.
        /// CategoryIds phải thuộc Writing Part 1/2/3.
        /// Trả về WritingSessionDto với đầy đủ câu hỏi và media.
        /// </summary>
        [HttpPost("writing/start")]
        [ProducesResponseType(typeof(WritingSessionDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Start(
            [FromBody] StartWritingRequest request,
            CancellationToken cancellationToken)
        {
            if (request.CategoryIds == null || request.CategoryIds.Count == 0)
                return BadRequest("Vui lòng chọn ít nhất một phần Writing.");

            var session = await _mediator.Send(new StartWritingPracticeCommand(
                UserId: UserId,
                CategoryIds: request.CategoryIds,
                IsTimed: request.IsTimed,
                TimeLimitMinutes: request.TimeLimitMinutes
            ), cancellationToken);

            return CreatedAtAction(nameof(GetResult),
                new { sessionId = session.SessionId }, session);
        }

        // ── Autosave draft ────────────────────────────────────────────

        /// <summary>
        /// Autosave câu trả lời đang gõ (gọi mỗi 10-30s từ frontend).
        /// Không trigger AI grading — chỉ lưu TextAnswer tạm thời.
        /// </summary>
        [HttpPost("writing/{sessionId:guid}/draft")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SaveDraft(
            Guid sessionId,
            [FromBody] SaveDraftRequest request,
            CancellationToken cancellationToken)
        {
            var saved = await _mediator.Send(new SaveWritingDraftCommand(
                SessionId: sessionId,
                UserId: UserId,
                QuestionId: request.QuestionId,
                TextAnswer: request.TextAnswer,
                TimeSpentSeconds: request.TimeSpentSeconds
            ), cancellationToken);

            return saved ? NoContent() : NotFound();
        }

        // ── Submit ────────────────────────────────────────────────────

        /// <summary>
        /// Nộp toàn bộ bài Writing.
        /// Enqueue AI grading jobs ngay lập tức (Hangfire).
        /// Trả về WritingSessionResultDto với IsFullyGraded = false
        /// — client cần polling GET /result để biết khi nào xong.
        /// </summary>
        [HttpPost("writing/{sessionId:guid}/submit")]
        [ProducesResponseType(typeof(WritingSessionResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Submit(
            Guid sessionId,
            [FromBody] SubmitWritingSessionRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Answers == null || request.Answers.Count == 0)
                return BadRequest("Không có câu trả lời nào được nộp.");

            try
            {
                var result = await _mediator.Send(new SubmitWritingSessionCommand(
                    SessionId: sessionId,
                    UserId: UserId,
                    Answers: request.Answers,
                    TotalTimeSeconds: request.TotalTimeSeconds
                ), cancellationToken);

                return Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already submitted"))
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // ── Result (polling) ──────────────────────────────────────────

        /// <summary>
        /// Lấy kết quả Writing session.
        /// Client nên polling mỗi 5-10s khi IsFullyGraded = false.
        /// Khi IsFullyGraded = true, OverallScore sẽ có giá trị (thang 0-200).
        /// </summary>
        [HttpGet("writing/{sessionId:guid}/result")]
        [ProducesResponseType(typeof(WritingSessionResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetResult(
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetWritingResultQuery(sessionId, UserId),
                cancellationToken);

            // Gợi ý client polling interval qua header
            if (!result.IsFullyGraded)
                Response.Headers.Append("X-Grading-Status", "pending");

            return Ok(result);
        }

        // ── Review ────────────────────────────────────────────────────

        /// <summary>
        /// Xem lại bài làm: câu hỏi + bài viết + AI feedback chi tiết.
        /// Chỉ hiển thị đầy đủ sau khi IsFullyGraded = true.
        /// </summary>
        [HttpGet("writing/{sessionId:guid}/review")]
        [ProducesResponseType(typeof(WritingReviewDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Review(
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            var review = await _mediator.Send(
                new GetWritingReviewQuery(sessionId, UserId),
                cancellationToken);

            return Ok(review);
        }
    }

    // ── Request models ────────────────────────────────────────────

    public class StartWritingRequest
    {
        public List<Guid> CategoryIds { get; set; } = new();
        public bool IsTimed { get; set; } = true;
        public int? TimeLimitMinutes { get; set; } = 60;
    }

    public class SaveDraftRequest
    {
        public Guid QuestionId { get; set; }
        public string TextAnswer { get; set; } = string.Empty;
        public int TimeSpentSeconds { get; set; }
    }

}
