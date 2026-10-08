using App.Application.Categories.Queries;
using App.Application.DTOs;
using App.Application.Exams.Commands;
using App.Application.Exams.Queries;
using App.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    [ApiController]
    [Route("api/exams")]
    [Authorize(Roles = "Admin")]
    public class ExamsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ExamsController(IMediator mediator)
        {
            _mediator = mediator;
        }


        // Api : Lấy toàn bộ câu hỏi
        [HttpGet("")]
        [AllowAnonymous]
        public async Task<IActionResult> GetList([FromQuery] GetExamQuery query)
        {
            query.PublishedOnly = !User.IsInRole("Admin");
            var result = await _mediator.Send(query);
            return Ok(new
            {
                success = true,
                data = result
            });
        }


        [HttpGet("home")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetHomeExams([FromQuery] GetHomeExamsQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        // GET : Lấy đề thi chi tiết
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {

            var query = new GetExamDetailQuery(id);
            var result = await _mediator.Send(query);

            return Ok(new
            {
                success = true,
                data = result,
                message = "Lấy thông tin người đề thành công"
            });

        }

        [HttpGet("{id:guid}/public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublic(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPublicExamQuery(id), cancellationToken);
            return Ok(new { success = true, data = result });
        }



        // UC-22.1: TẠO ĐỀ THI TRỐNG
        // POST /api/exams
        // ============================================
        [HttpPost]
        public async Task<ActionResult> CreateExam(
            [FromBody] CreateExamCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }

        // ============================================
        // UC-22.2: THÊM SECTION VÀO ĐỀ
        // POST /api/exams/{examId}/sections
        // ============================================
        [HttpDelete("{examId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteExam(
            Guid examId,
            [FromQuery] bool hardDelete = false)
        {
            var command = new DeleteExamCommand
            {
                ExamId = examId,
                HardDelete = hardDelete
            };

            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }



        // ============================================
        // UPDATE: CẬP NHẬT THÔNG TIN ĐỀ THI
        // PUT /api/exams/{examId}
        // ============================================
        [HttpPut("{examId}")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateExam(
            Guid examId,
            [FromBody] UpdateExamCommand command)
        {
            command.ExamId = examId;
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }


        // ============================================
        // DUPLICATE: NHÂN BẢN ĐỀ THI
        // POST /api/exams/{examId}/duplicate
        // ============================================
        [HttpPost("{examId}/duplicate")]
        public async Task<ActionResult<ApiResponse<Guid>>> DuplicateExam(
            Guid examId,
            [FromBody] DuplicateExamCommand request)
        {
            var command = new DuplicateExamCommand
            {
                SourceExamId = examId,
                NewCode = request.NewCode,
                NewTitle = request.NewTitle
            };

            var newExamId = await _mediator.Send(command);
            return Ok(new { success = true, data = newExamId, message = "Nhân bản đề thi thành công" });
        }


        // PUT /api/exams/sections/{sectionId}
        [HttpGet("{examId:guid}/preview")]
        public async Task<IActionResult> Preview(
            Guid examId,
            [FromQuery] bool showCorrectAnswers = true)
        {
            var query = new GetExamPreviewQuery
            {
                ExamId = examId,
                ShowCorrectAnswers = showCorrectAnswers
            };
            var result = await _mediator.Send(query);
            return Ok(new { success = true, data = result });
        }



        // POST /api/exams/{examId}/publish
        [HttpPost("{examId:guid}/publish")]
        public async Task<IActionResult> Publish(Guid examId)
        {
            await _mediator.Send(new PublishExamCommand { ExamId = examId });
            return Ok(new { success = true, message = "Xuất bản đề thi thành công" });
        }

        // PATCH /api/exams/{examId}/status
        [HttpPatch("{examId:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid examId,
            [FromBody] ChangeExamStatusCommand command)
        {
            command.ExamId = examId;
            await _mediator.Send(command);
            return Ok(new { success = true, message = "Cập nhật trạng thái thành công" });
        }



        // Get all exam full test
        [HttpGet("full-tests")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ExamSummaryDto>>> GetFullTests()
        {
            var query = new GetFullTestsQuery();
            var result = await _mediator.Send(query);
            return Ok(result);
        }

    }
}
                                                          
