using App.Application.Leaderboards.Queries;
using App.Application.Students.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetDashboardInfo()
        {
            var query = new GetDashboardInfoQuery();
            var result = await _mediator.Send(query);
            return Ok(new { success = true, data = result });
        }


        /// <summary>
        /// Lấy danh sách bảng xếp hạng
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        [HttpGet("leaderboard")]
        public async Task<ActionResult<LeaderboardResult>> GetLeaderboard([FromQuery] int limit = 10)
        {
            var query = new GetLeaderboardQuery(limit);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
