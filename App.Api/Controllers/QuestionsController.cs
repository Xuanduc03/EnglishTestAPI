using App.Application.DTOs.Questions;
using App.Application.ExamDigitize.Commands;
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
        public async Task<IActionResult> CreateQuestionGroup(
     [FromForm] CreateQuestionGroupRequest request)
        {
            // Parse QuestionsJson
            List<CreateQuestionDto> questions = [];

            if (!string.IsNullOrWhiteSpace(request.QuestionsJson))
            {
                try
                {
                    var json = request.QuestionsJson.Trim();
                    questions = System.Text.Json.JsonSerializer.Deserialize<List<CreateQuestionDto>>(
                        json,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true,
                            ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                        }) ?? [];
                }
                catch (System.Text.Json.JsonException ex)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"QuestionsJson không hợp lệ: {ex.Message}",
                        // Log raw để debug
                        receivedJson = request.QuestionsJson
                    });
                }
            }

            var command = new CreateQuestionGroupCommand
            {
                CategoryId = request.CategoryId,
                GroupContent = request.GroupContent,
                GroupAudioUrl = request.GroupAudioUrl,
                GroupImageUrl = request.GroupImageUrl,
                DifficultyId = request.DifficultyId,
                Explanation = request.Explanation,
                Transcript = request.Transcript,
                MediaJson = request.MediaJson,
                GroupAudioFile = request.GroupAudioFile,
                GroupImageFile = request.GroupImageFile,
                Tags = request.Tags ?? [],
                Questions = questions,
            };

            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result, message = "Tạo câu hỏi nhóm thành công" });
        }


        [HttpPut("groups/{id}")]
        public async Task<IActionResult> UpdateQuestionGroup(Guid id, [FromForm] UpdateQuestionGroupCommand command)
        {

            command.Id = id;
            var result = await _mediator.Send(command);
            return Ok(result);


        }

        [HttpPost("singles")]
        public async Task<IActionResult> CreateSingleQuestion([FromForm] CreateSingleQuestionCommand command)
        {

            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result, message = "Tạo câu hỏi đơn thành công" });

        }

        [HttpPut("singles/{id}")]
        public async Task<IActionResult> UpdateSingleQuestion(
            Guid id,
            [FromForm] UpdateSingleQuestionCommand command)
        {
            command.Id = id; // Gán lại ID cho chắc
            var updatedId = await _mediator.Send(command);
            return Ok(updatedId);
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

        // GET /api/questions/hierarchy
        // chỉ dùng cho query câu hỏi nhóm 
        [HttpGet("hierarchy")]
        public async Task<IActionResult> GetHierarchy([FromQuery] GetQuestionsHierarchyQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(new { success = true, data = result });
        }


        [HttpPost("preview-excel-zip")]
        [RequestSizeLimit(200_000_000)] // 200 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
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

        /// OCR QUESTION MODULE 
        /// POST /api/exam-digitize/extract
        /// Upload ảnh → Gemini extract → trả JSON preview
        [HttpPost("extract")]
        public async Task<IActionResult> Extract(
    [FromForm] List<IFormFile> files,
    [FromForm] string examType = "IELTS_READING")
        {
            if (files == null || !files.Any())
                return BadRequest(new { message = "Vui lòng upload ít nhất 1 ảnh" });

            if (files.Count > 10)
                return BadRequest(new { message = "Tối đa 10 ảnh mỗi lần" });

            var result = await _mediator.Send(new UploadAndExtractCommand
            {
                Files = files,
                ExamType = examType,
            });

            return Ok(new { success = true, data = result });
        }

        /// POST /api/exam-digitize/save
        /// Admin confirm → lưu vào DB
        [HttpPost("save-extract")]
        public async Task<IActionResult> Save([FromBody] SaveDigitizedExamCommand command)
        {
            var groupId = await _mediator.Send(command);
            return Ok(new { success = true, data = new { groupId } });
        }
    }
}
