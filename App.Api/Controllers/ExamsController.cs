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
        public async Task<IActionResult> GetList([FromQuery] GetExamQuery query)
        {
            try
            {
                var result = await _mediator.Send(query);
                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET : Lấy đề thi chi tiết
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
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

            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }



        // UC-22.1: TẠO ĐỀ THI TRỐNG
        // POST /api/exams
        // ============================================
        [HttpPost]
        public async Task<ActionResult> CreateExam(
            [FromBody] CreateExamCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
           
        }

        // ============================================
        // UC-22.2: THÊM SECTION VÀO ĐỀ
        // POST /api/exams/{examId}/sections
        // ============================================
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
            return Ok(new { success = true, data = "Nhân bản đề thi thành công" });
        }


        // PUT /api/exams/sections/{sectionId}
        [HttpPut("sections/{sectionId}")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateSection(
            Guid sectionId,
            [FromBody] UpdateExamSectionCommand command)
        {
            command.SectionId = sectionId;
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = "Cập nhật phần thi thành công" });
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
            return Ok(new { success = true, data = "Xóa phần thi thành công" });
        }


    }
}
