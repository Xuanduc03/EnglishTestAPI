using App.Application.ExamAttempts.Commands;
using App.Application.ExamAttempts.Commands.IELTS;
using App.Application.ExamAttempts.Queries.IELTS;
using App.Application.Services.Interface;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        [AllowAnonymous]
        [EnableRateLimiting("exam-start")]
        public async Task<IActionResult> Start([FromBody] IeltsStartExamCommand command)
        {
            command.UserId = _currentUserService.UserId ?? Guid.Empty;
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }


        // POST /api/ielts/attempts/{attemptId}/submit
        [HttpPost("{attemptId}/submit")]
        [AllowAnonymous]
        public async Task<IActionResult> Submit([FromRoute] Guid attemptId)
        {
            var command = new IeltsSubmitExamCommand
            {
                AttemptId = attemptId,
                UserId = _currentUserService.UserId ?? Guid.Empty,
                GuestToken = Request.Headers["X-Guest-Token"].ToString(),
            };
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }


        // POST /api/ielts/attempts/{attemptId}/answers/fill-in
        [HttpPost("{attemptId}/answers/fill-in")]
        [AllowAnonymous]
        public async Task<IActionResult> SaveFillIn(
            [FromRoute] Guid attemptId,
            [FromBody] IeltsSaveFillInAnswerCommand command)
        {
            command.AttemptId = attemptId;
            command.UserId = _currentUserService.UserId ?? Guid.Empty;
            command.GuestToken = Request.Headers["X-Guest-Token"].ToString();
            await _mediator.Send(command);
            return Ok(new { success = true });
        }

        // POST /api/ielts/attempts/{attemptId}/answers/mcq
        [HttpPost("{attemptId}/answers/mcq")]
        [AllowAnonymous]
        public async Task<IActionResult> SaveMcq(
            [FromRoute] Guid attemptId,
            [FromBody] IeltsSaveMcqAnswerCommand command)
        {
            command.AttemptId = attemptId;
            command.UserId = _currentUserService.UserId ?? Guid.Empty;
            command.GuestToken = Request.Headers["X-Guest-Token"].ToString();
            await _mediator.Send(command);
            return Ok(new { success = true });
        }

        // API review lại bài làm bài thi IELTS
        [HttpGet("{attemptId}/review")]
        [AllowAnonymous]
        public async Task<IActionResult> Review(Guid attemptId)
        {
            var result = await _mediator.Send(new IeltsReviewExamQuery
            {
                AttemptId = attemptId,
                UserId = _currentUserService.UserId,
                GuestToken = Request.Headers["X-Guest-Token"].ToString(),
            });
            return Ok(new { success = true, data = result });
        }

        private Guid CurrentUserId => _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Invalid user token");
    }
}
