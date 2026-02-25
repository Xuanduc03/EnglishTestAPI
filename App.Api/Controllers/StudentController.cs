using App.Application.Commands;
using App.Application.Interfaces;
using App.Application.Queries;
using App.Application.Students.Commands;
using App.Application.Students.Queries;
using App.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace App.Api.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICloudinaryService _cloudinary;
        private readonly IAppDbContext _context;
        private readonly ILogger<StudentController> _logger;
        public StudentController(IAppDbContext context, IMediator mediator, ILogger<StudentController> logger, ICloudinaryService cloudinary)
        {
            _context = context;
            _mediator = mediator;
            _logger = logger;
            _cloudinary = cloudinary;
        }

        /// <summary>
        /// Get all classes with filtering and pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStudents([FromQuery] GetAllStudentsQuery query)
        {


            var result = await _mediator.Send(query);
            return Ok(new { success = true, data = result });

        }
        #region using for student
        [HttpGet("me")]
        public async Task<IActionResult> GetProfileStudent()
        {

            var userId = GetCurrentUserId();
            var query = new GetStudentProfile(userId);
            var result = await _mediator.Send(query);
            return Ok(new { success = true, data = result });

        }

        [HttpPut("me")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateStudentProfile([FromForm] UpdateStudentProfileCommand request)
        {
            // Lấy user hiện tại để lấy avatar public id cũ (nếu có)
            var currentUser = await GetCurrentUser();
            string? currentAvatarPublicId = currentUser?.AvatarPublicId;

            // Nếu có avatar mới upload, xử lý upload và xóa avatar cũ
            if (request.AvatarFile != null && request.AvatarFile.Length > 0)
            {
                // Upload avatar mới
                var upload = await _cloudinary.UploadImageAsync(
                    request.AvatarFile,
                    "avatars/students"
                );

                // Gán URL và PublicId mới vào command
                request = request with
                {
                    AvatarUrl = upload.Url,
                    AvatarPublicId = upload.PublicId
                };

                // Xóa avatar cũ nếu tồn tại
                if (!string.IsNullOrEmpty(currentAvatarPublicId))
                {
                    try
                    {
                        await _cloudinary.DeleteAsync(currentAvatarPublicId);
                    }
                    catch
                    {
                        // Log error nhưng không làm fail request
                        _logger.LogWarning($"Failed to delete old avatar: {currentAvatarPublicId}");
                    }
                }
            }
            else
            {
                // Nếu không có file mới, giữ lại giá trị cũ
                request = request with
                {
                    AvatarUrl = currentUser?.AvatarUrl,
                    AvatarPublicId = currentUser?.AvatarPublicId
                };
            }

            // Thiết lập UserId
            request = request with { UserId = GetCurrentUserId() };

            var result = await _mediator.Send(request);

            return Ok(new
            {
                success = true,
                data = result
            });

        }

        #endregion

        [HttpGet("{id}/chi-tiet")]
        public async Task<IActionResult> GetStudentById(Guid id)
        {

            var query = new GetStudentByIdQuery { Id = id };
            var result = await _mediator.Send(query);
            return Ok(new { success = true, data = result });

        }

        [HttpPost("")]
        public async Task<IActionResult> CreateStudent(CreateStudentCommand request)
        {

            var result = await _mediator.Send(request);

            return Ok(new { success = true, data = result });

        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] UpdateStudentCommand command)
        {

            command.Id = id;
            var result = await _mediator.Send(command);

            return Ok(new { success = true, data = result });

        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> DeleteStudent([FromRoute] Guid id)
        {

            var command = new DeleteStudentCommand
            {
                id = id,
                ids = null, // Xóa đơn thì list này null
                DeletedBy = GetCurrentUserId() // Hàm lấy ID người xóa
            };

            var result = await _mediator.Send(command);

            if (!result)
                return NotFound(new { success = false, message = "Student not found or already deleted." });

            return Ok(new { success = true, message = "Student deleted successfully." });


        }

        [HttpPost("bulk-delete")] // Dùng POST an toàn hơn DELETE khi có Body
        public async Task<IActionResult> BulkDeleteStudents([FromBody] List<Guid> ids)
        {

            if (ids == null || !ids.Any())
            {
                return BadRequest(new { success = false, message = "No IDs provided." });
            }

            var command = new DeleteStudentCommand
            {
                id = null,
                ids = ids,
                DeletedBy = GetCurrentUserId()
            };

            var result = await _mediator.Send(command);

            return Ok(new { success = true, message = $"{ids.Count} students processed for deletion." });

        }
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user token");
            }

            return userId;
        }

        private async Task<User> GetCurrentUser()
        {
            var userId = GetCurrentUserId();
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}
