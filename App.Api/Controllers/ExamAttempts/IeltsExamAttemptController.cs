using App.Application.ExamAttempts.Commands;
using App.Application.ExamAttempts.Commands.IELTS;
using App.Application.ExamAttempts.Queries.IELTS;
using App.Application.Services.Interface;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers.ExamAttempts
{
    [ApiController]
    [Route("api/ielts/attempts")]
    [Authorize]
    public class IeltsExamAttemptController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public IeltsExamAttemptController(IMediator mediator, ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _currentUserService = currentUserService;
        }

        // POST /api/ielts/attempts/start
        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] IeltsStartExamCommand command)
        {
            command.UserId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User not found");
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }


        // POST /api/ielts/attempts/{attemptId}/submit
        [HttpPost("{attemptId}/submit")]
        public async Task<IActionResult> Submit([FromRoute] Guid attemptId)
        {
            var UserId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User not found");
            var command = new IeltsSubmitExamCommand
            {
                AttemptId = attemptId,
                UserId = UserId,
            };
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }


        // POST /api/ielts/attempts/{attemptId}/answers/fill-in
        [HttpPost("{attemptId}/answers/fill-in")]
        public async Task<IActionResult> SaveFillIn(
            [FromRoute] Guid attemptId,
            [FromBody] IeltsSaveFillInAnswerCommand command)
        {
            command.AttemptId = attemptId;
            command.UserId = _currentUserService.UserId
                ?? throw new UnauthorizedAccessException("User not found");
            await _mediator.Send(command);
            return Ok(new { success = true });
        }

        // POST /api/ielts/attempts/{attemptId}/answers/mcq
        [HttpPost("{attemptId}/answers/mcq")]
        public async Task<IActionResult> SaveMcq(
            [FromRoute] Guid attemptId,
            [FromBody] IeltsSaveMcqAnswerCommand command)
        {
            command.AttemptId = attemptId;
            command.UserId = _currentUserService.UserId
                ?? throw new UnauthorizedAccessException("User not found");
            await _mediator.Send(command);
            return Ok(new { success = true });
        }

        // API review lại bài làm bài thi IELTS
        [HttpGet("{attemptId}/review")]
        public async Task<IActionResult> Review(Guid attemptId)
        {
            var result = await _mediator.Send(new IeltsReviewExamQuery
            {
                AttemptId = attemptId,
                UserId = _currentUserService.UserId,
            });
            return Ok(new { success = true, data = result });
        }
    }
}
