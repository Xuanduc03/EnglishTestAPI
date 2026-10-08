using App.Application.DTOs.Questions;
using App.Application.ExamDigitize.Commands;
using App.Application.Questions.Commands;
using App.Application.Questions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace App.Api.Controllers
{
    [ApiController]
    [Route("api/questions")]
    [Authorize(Roles = "Admin")]
    public class QuestionImportsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public QuestionImportsController(IMediator mediator) => _mediator = mediator;
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
        /// POST /api/questions/extract
        /// Upload ảnh → Gemini extract → trả JSON preview
        [HttpPost("extract")]
        public async Task<IActionResult> Extract(
            [FromForm] List<IFormFile> files,
            [FromForm] string examType = "IELTS_READING",
            [FromForm] string? passageOnly = null,   // nhận string vì FormData gửi "true"/"false"
            [FromForm] string? questionsOnly = null,
            [FromForm] string? passageContent = null)
        {
            if (files == null || !files.Any())
                return BadRequest(new { message = "Vui lòng upload ít nhất 1 ảnh" });
            if (files.Count > 10)
                return BadRequest(new { message = "Tối đa 10 ảnh mỗi lần" });

            var result = await _mediator.Send(new UploadAndExtractCommand
            {
                Files = files,
                ExamType = examType,
                PassageOnly = passageOnly == "true",  // parse thủ công
                QuestionsOnly = questionsOnly == "true",
                PassageContent = passageContent,
            });

            return Ok(new { success = true, data = result });
        }

        // ── Request model cho [FromForm] ──────────────────────────
        public class SaveDigitizedExamRequest
        {
            [Required] public Guid CategoryId { get; set; }
            public Guid? DifficultyId { get; set; }

            // Files
            public IFormFile? AudioFile { get; set; }
            public IFormFile? ImageFile { get; set; }

            // URL fallback
            public string? AudioUrl { get; set; }
            public string? ImageUrl { get; set; }

            // ExtractedData dưới dạng JSON string (vì FormData không support nested object)
            [Required]
            [FromForm(Name = "extractedData")]
            public string? ExtractedDataJson { get; set; }

            public List<string>? Tags { get; set; }
        }

        /// POST /api/questions/save-extract
        /// Admin confirm → lưu vào DB
        [HttpPost("save-extract")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SaveExtract(
        [FromForm] SaveDigitizedExamRequest request,
        CancellationToken ct)
        {
            // Deserialize ExtractedData từ JSON string
            if (string.IsNullOrWhiteSpace(request.ExtractedDataJson))
                return BadRequest("extractedData is required");

            ExtractedExamDto extractedData;
            try
            {
                extractedData = JsonSerializer.Deserialize<ExtractedExamDto>(
                    request.ExtractedDataJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? throw new Exception("null");
            }
            catch
            {
                return BadRequest("extractedData JSON is invalid");
            }

            var command = new SaveDigitizedExamCommand
            {
                CategoryId = request.CategoryId,
                DifficultyId = request.DifficultyId,
                AudioFile = request.AudioFile,   // IFormFile
                ImageFile = request.ImageFile,   // IFormFile
                AudioUrl = request.AudioUrl,    // string fallback
                ImageUrl = request.ImageUrl,    // string fallback
                Tags = request.Tags ?? [],
                ExtractedData = extractedData,
            };

            var groupId = await _mediator.Send(command, ct);
            return Ok(new { success = true, data = new { groupId } });
        }


        /// POST /api/questions/export
        /// Thực hiện xuất toàn bộ bank question ra excel 
        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel([FromQuery] ExportQuestionsQuery query)
        {
            var file = await _mediator.Send(query);

            return File(
                file.Content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                file.FileName
            );
        }
    }
}
