using App.Application.Admin.Queries;
using App.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    [ApiController]
    [Route("api/admin/stats")]
    [Authorize(Roles = "Admin")]
    public class AdminStatsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AdminStatsController(IMediator mediator) => _mediator = mediator;

        // Helper parse query params thành DateRangeFilter
        private DateRangeFilter BuildFilter(
            DateTime? from, DateTime? to, Granularity gran = Granularity.Day)
            => new(from, to, gran);

        // GET /api/admin/stats/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] Granularity gran = Granularity.Day)
        {
            var result = await _mediator.Send(
                new GetAdminDashboardStatsQuery(BuildFilter(from, to, gran)));
            return Ok(new { success = true, data = result });
        }

        // GET /api/admin/stats/users/growth
        [HttpGet("users/growth")]
        public async Task<IActionResult> UserGrowth(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] Granularity gran = Granularity.Day)
        {
            var result = await _mediator.Send(
                new GetUserGrowthStatsQuery(BuildFilter(from, to, gran)));
            return Ok(new { success = true, data = result });
        }

        // GET /api/admin/stats/exams/submissions
        [HttpGet("exams/submissions")]
        public async Task<IActionResult> ExamSubmissions(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] Granularity gran = Granularity.Day)
        {
            var result = await _mediator.Send(
                new GetExamSubmissionStatsQuery(BuildFilter(from, to, gran)));
            return Ok(new { success = true, data = result });
        }

        // GET /api/admin/stats/scores/distribution?examId=...
        [HttpGet("scores/distribution")]
        public async Task<IActionResult> ScoreDistribution(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] Guid? examId)
        {
            var result = await _mediator.Send(
                new GetScoreDistributionQuery(BuildFilter(from, to), examId));
            return Ok(new { success = true, data = result });
        }

        // GET /api/admin/stats/exams/pass-rate?top=10
        [HttpGet("exams/pass-rate")]
        public async Task<IActionResult> PassRate(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int top = 10)
        {
            var result = await _mediator.Send(
                new GetPassRateByExamQuery(BuildFilter(from, to), top));
            return Ok(new { success = true, data = result });
        }

        // GET /api/admin/stats/completion-time
        [HttpGet("completion-time")]
        public async Task<IActionResult> CompletionTime(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var result = await _mediator.Send(
                new GetAverageCompletionTimeQuery(BuildFilter(from, to)));
            return Ok(new { success = true, data = result });
        }

        // GET /api/admin/stats/exams/top?top=10
        [HttpGet("exams/top")]
        public async Task<IActionResult> TopExams(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int top = 10)
        {
            var result = await _mediator.Send(
                new GetTopExamsQuery(BuildFilter(from, to), top));
            return Ok(new { success = true, data = result });
        }

        // GET /api/admin/stats/users/top?top=10
        [HttpGet("users/top")]
        public async Task<IActionResult> TopUsers(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int top = 10)
        {
            var result = await _mediator.Send(
                new GetTopUsersQuery(BuildFilter(from, to), top));
            return Ok(new { success = true, data = result });
        }

        // GET /api/admin/stats/skills
        [HttpGet("skills")]
        public async Task<IActionResult> SkillStats(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var result = await _mediator.Send(
                new GetSkillStatsQuery(BuildFilter(from, to)));
            return Ok(new { success = true, data = result });
        }

        // GET /api/admin/stats/activity?take=20
        [HttpGet("activity")]
        public async Task<IActionResult> RecentActivity([FromQuery] int take = 20)
        {
            var result = await _mediator.Send(new GetRecentActivityQuery(take));
            return Ok(new { success = true, data = result });
        }
    }
}
