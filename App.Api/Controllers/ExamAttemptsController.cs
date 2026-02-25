using App.Application.ExamAttempts.Commands;
using App.Application.Practices.Queries;
using App.Application.Services.Interface;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// API 
    /// </summary>
    [Route("api/exam-attempts")]
    [ApiController]
    public class ExampAttemptController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public ExampAttemptController(IMediator mediator, ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost("start")]
        public async Task<IActionResult> StartExamAttempts([FromBody]StartExamCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }


        /// POST /api/exam-attempts/{attemptId}/submit <summary>
        /// Command : Nộp bài + chấm điểm
        [HttpPost("{attemptId}/submit")]
        public async Task<IActionResult> Submit(Guid attemptId)
        {
            var command = new SubmitExamCommand();
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost("auto-save")]
        public async Task<IActionResult> AutoSave([FromBody] SaveAnswerCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="attemptId"></param>
        /// <returns></returns>
        [HttpGet("{attemptId}/resume")]
        public async Task<IActionResult> ResumeExam(Guid attemptId)
        {
            var command = new ResumeExamCommand(attemptId);
            var result = await _mediator.Send(command);
            return Ok(new { success = true, data = result });
        }



        /// <summary>
        /// Endpoint: GET /api/exam-attempts/in-progress
        /// Trả về danh sách các bài thi đang làm dở của user hiện tại, kèm tiêu đề và tiến độ.
        /// </summary>
        /// <returns></returns>
        [HttpGet("in-progress")]
        public async Task<IActionResult> GetInProgressAttempts()
        {
            var query = new GetInProgressPracticesQuery();
            var result = await _mediator.Send(query);
            return Ok(new { success = true, data = result });
        }
    }
}
