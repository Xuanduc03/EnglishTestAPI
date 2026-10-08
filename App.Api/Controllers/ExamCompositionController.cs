using App.Application.DTOs;
using App.Application.Exams.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    [ApiController]
    [Route("api/exams")]
    [Authorize(Roles = "Admin")]
    public class ExamCompositionController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ExamCompositionController(IMediator mediator) => _mediator = mediator;

        [HttpPost("{examId}/sections")]
        public async Task<ActionResult<ApiResponse<Guid>>> AddSection(
            Guid examId,
            [FromBody] AddExamSectionCommand command)
        {
            command.ExamId = examId; // Override từ route
            var sectionId = await _mediator.Send(command);
            return Ok(new { success = true, data = sectionId });
        }

        // ============================================
        // UC-22.3: THÊM CÂU HỎI VÀO SECTION
        // POST /api/exams/{examId}/sections/{sectionId}/questions
        // ============================================
        [HttpPost("{examId}/sections/{sectionId}/questions")]
        public async Task<ActionResult<ApiResponse<List<Guid>>>> AddQuestionsToSection(
            Guid examId,
            Guid sectionId,
            [FromBody] AddQuestionsToSectionCommand command)
        {
            command.ExamId = examId;
            command.SectionId = sectionId;

            var examQuestionIds = await _mediator.Send(command);
            return Ok(new { success = true, data = examQuestionIds });
        }

        // ============================================
        // UC-22.4: SẮP XẾP LẠI CÂU HỎI
        // PUT /api/exams/{examId}/sections/{sectionId}/questions/reorder
        // ============================================
        [HttpPut("{examId}/sections/{sectionId}/questions/reorder")]
        public async Task<ActionResult<ApiResponse<bool>>> ReorderQuestions(
            Guid examId,
            Guid sectionId,
            [FromBody] ReorderExamQuestionsCommand command)
        {
            command.ExamId = examId;
            command.SectionId = sectionId;

            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }

        // ============================================
        // UC-22.5: XÓA CÂU HỎI KHỎI ĐỀ
        // DELETE /api/exams/{examId}/questions/{examQuestionId}
        // ============================================
        [HttpDelete("{examId}/questions/{examQuestionId}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveQuestion(
            Guid examId,
            Guid examQuestionId)
        {
            var command = new RemoveQuestionFromExamCommand
            {
                ExamId = examId,
                ExamQuestionId = examQuestionId
            };

            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }


        // xóa nhiều câu hỏi khỏi section 
        [HttpDelete("{examId}/questions")]
        public async Task<IActionResult> BulkDeleteQuestions(Guid examId, [FromBody] List<Guid> examQuestionIds)
        {
            var command = new BulkDeleteExamQuestionsCommand
            {
                ExamId = examId,
                ExamQuestionIds = examQuestionIds
            };
            var result = await _mediator.Send(command);
            return Ok(new { success = result });
        }

        // ============================================
        // UC-22.6: CẬP NHẬT ĐIỂM SỐ CÂU HỎI
        // PATCH /api/exams/{examId}/questions/{examQuestionId}/point
        //// ============================================
        [HttpPatch("{examId}/questions/{examQuestionId}/point")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateQuestionPoint(
            Guid examId,
            Guid examQuestionId,
            [FromBody] UpdateQuestionPointCommand request)
        {
            var command = new UpdateQuestionPointCommand
            {
                ExamId = examId,
                ExamQuestionId = examQuestionId,
                NewPoint = request.NewPoint
            };

            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }

        // ============================================
        // DELETE: XÓA ĐỀ THI (SOFT DELETE)
        // DELETE /api/exams/{examId}
        // ============================================
        [HttpPut("sections/{sectionId}")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateSection(
            Guid sectionId,
            [FromBody] UpdateExamSectionCommand command)
        {
            command.SectionId = sectionId;
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result, message = "Cập nhật phần thi thành công" });
        }

        // DELETE /api/exams/{examId}/sections/{sectionId}
        [HttpDelete("{examId}/sections/{sectionId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteSection(
            Guid examId,
            Guid sectionId)
        {
            var command = new DeleteExamSectionCommand
            {
                ExamId = examId,
                SectionId = sectionId
            };

            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result, message = "Xóa phần thi thành công" });
        }


        // GET /api/exams/{examId}/preview
        // Xem trước toàn bộ đề thi

    }
}
