using App.Application.DTOs.Questions;
using App.Application.Questions.Commands;
using App.Application.Questions.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;


namespace App.Api.Controllers
{
    [ApiController]
    [Route("/api/questions")]
    public class QuestionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public QuestionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        //get all 
        [HttpGet("")]
        public async Task<IActionResult> GetAllQuestion([FromQuery] GetAllQuestionsQuery request)
        {
            try
            {
                var result = await _mediator.Send(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("single/{id}")]
        public async Task<IActionResult> GetSingleQuestionDetail(Guid id)
        {
            var result = await _mediator.Send(
                new GetSingleQuestionDetailQuery(id)
            );

            return Ok(result);
        }


        [HttpGet("group/{id}")]
        public async Task<IActionResult> GetQuestionGroupDetail(Guid id)
        {
            var result = await _mediator.Send(
                new GetQuestionGroupDetailQuery(id)
            );

            return Ok(result);
        }


        // tạo câu hỏi nhóm
        [HttpPost("groups")]
        public async Task<IActionResult> CreateQuestionGroup([FromForm] CreateQuestionGroupCommand command)
        {

            try
            {
                var result = await _mediator.Send(command);
                return Ok(new { success = true, data = result, message = "Tạo câu hỏi nhóm thành công" });
            }
            catch (Exception ex)
            {
                // Log ex
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        [HttpPut("groups/{id}")]
        public async Task<IActionResult> UpdateQuestionGroup(Guid id, [FromForm] UpdateQuestionGroupCommand command)
        {
            try
            {
                command.Id = id;

                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                });
            }

        }

        [HttpPost("singles")]
        public async Task<IActionResult> CreateSingleQuestion([FromForm] CreateSingleQuestionCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);

                return Ok(new { sucess = true, data = result, message = "Tạo câu hỏi đơn thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        [HttpPut("singles/{id}")]
        public async Task<IActionResult> UpdateSingleQuestion(
            Guid id,
            [FromForm] UpdateSingleQuestionCommand command)
        {

            try
            {
                command.Id = id; // Gán lại ID cho chắc
                var updatedId = await _mediator.Send(command);
                return Ok(updatedId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        [HttpDelete("singles/{id}")]
        public async Task<IActionResult> DeleteQuestion(Guid id, [FromQuery] bool hardDelete = false)
        {
            var success = await _mediator.Send(new DeleteQuestionCommand(id, hardDelete));
            return NoContent();
        }

        [HttpDelete("groups/{id}")]
        public async Task<IActionResult> DeleteQuestionGroup(Guid id, [FromQuery] bool hardDelete = false)
        {
            var success = await _mediator.Send(new DeleteQuestionGroupCommand(id, hardDelete));
            return NoContent();
        }


        [HttpPost("preview-excel-zip")]
        public async Task<IActionResult> PreviewExcelZip(
        [FromForm] PreviewQuestionExcelCommand request,
        CancellationToken cancellationToken)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest("File ZIP không hợp lệ");
            }

            var result = await _mediator.Send(
                new PreviewQuestionExcelCommand
                {
                    File = request.File
                },
                cancellationToken
            );

            return Ok(result);
        }

        [HttpPost("import-zip")]
        public async Task<IActionResult> ImportQuestions(
            [FromForm] ImportQuestionExcelCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest("File ZIP không hợp lệ");
                }

                var result = await _mediator.Send(
                    new ImportQuestionExcelCommand
                    {
                        File = request.File
                    },
                    cancellationToken
                );

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }
    }
}
