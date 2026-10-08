using App.Application.Interfaces;
using App.Application.Services.Interface;
using App.Application.Students.Commands;
using App.Application.Students.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Api.Controllers;

[ApiController]
[Route("api/students")]
[Authorize]
public class StudentProfileController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IAppDbContext _context;
    private readonly ICloudinaryService _cloudinary;
    private readonly ILogger<StudentProfileController> _logger;

    public StudentProfileController(
        IMediator mediator,
        ICurrentUserService currentUser,
        IAppDbContext context,
        ICloudinaryService cloudinary,
        ILogger<StudentProfileController> logger)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _context = context;
        _cloudinary = cloudinary;
        _logger = logger;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfileStudent()
    {
        var result = await _mediator.Send(new GetStudentProfile(CurrentUserId));
        return Ok(new { success = true, data = result });
    }

    [HttpPut("me")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateStudentProfile([FromForm] UpdateStudentProfileCommand request)
    {
        var userId = CurrentUserId;
        var profile = await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.StudentProfile)
            .FirstOrDefaultAsync();

        if (profile == null)
            throw new KeyNotFoundException("Hồ sơ học viên không tồn tại");

        var oldAvatarPublicId = profile.AvatarPublicId;
        string? uploadedPublicId = null;

        if (request.AvatarFile is { Length: > 0 })
        {
            var upload = await _cloudinary.UploadImageAsync(request.AvatarFile, "avatars/students");
            uploadedPublicId = upload.PublicId;
            request = request with { AvatarUrl = upload.Url, AvatarPublicId = upload.PublicId };
        }
        else
        {
            request = request with { AvatarUrl = profile.AvatarUrl, AvatarPublicId = oldAvatarPublicId };
        }

        request = request with { UserId = userId };

        App.Application.DTO.StudentProfileDto result;
        try
        {
            result = await _mediator.Send(request);
        }
        catch
        {
            if (uploadedPublicId != null)
            {
                try { await _cloudinary.DeleteAsync(uploadedPublicId); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete unused avatar: {PublicId}", uploadedPublicId); }
            }
            throw;
        }

        if (uploadedPublicId != null && !string.IsNullOrEmpty(oldAvatarPublicId))
        {
            try { await _cloudinary.DeleteAsync(oldAvatarPublicId); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete old avatar: {PublicId}", oldAvatarPublicId); }
        }

        return Ok(new { success = true, data = result });
    }

    private Guid CurrentUserId => _currentUser.UserId
        ?? throw new UnauthorizedAccessException("Invalid user token");
}
