using App.Application.Commands;
using App.Application.Queries;
using App.Application.Services.Interface;
using App.Application.Students.Commands;
using App.Application.Students.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers;

[ApiController]
[Route("api/students")]
[Authorize(Roles = "Admin")]
public class StudentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public StudentController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetStudents([FromQuery] GetAllStudentsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("{id}/chi-tiet")]
    public async Task<IActionResult> GetStudentById(Guid id)
    {
        var result = await _mediator.Send(new GetStudentByIdQuery { Id = id });
        return Ok(new { success = true, data = result });
    }

    [HttpPost]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentCommand request)
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStudent(Guid id)
    {
        var result = await _mediator.Send(new DeleteStudentCommand
        {
            id = id,
            DeletedBy = CurrentUserId
        });

        if (!result)
            return NotFound(new { success = false, message = "Student not found or already deleted." });

        return Ok(new { success = true, message = "Student deleted successfully." });
    }

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDeleteStudents([FromBody] List<Guid> ids)
    {
        if (ids == null || ids.Count == 0)
            return BadRequest(new { success = false, message = "No IDs provided." });

        var result = await _mediator.Send(new DeleteStudentCommand
        {
            ids = ids,
            DeletedBy = CurrentUserId
        });

        if (!result)
            return NotFound(new { success = false, message = "No matching students found." });

        return Ok(new { success = true, message = "Students deleted successfully." });
    }

    private Guid CurrentUserId => _currentUser.UserId
        ?? throw new UnauthorizedAccessException("Invalid user token");
}
