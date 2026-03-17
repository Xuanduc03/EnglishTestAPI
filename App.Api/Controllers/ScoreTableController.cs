using App.Application.ScoreTables.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    [ApiController]
    [Route("/api/score-table")]
    public class ScoreTableController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ScoreTableController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // ── GET /api/score-tables?examId=xxx&skillType=Listening ──
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetScoreTablesQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(new { success = true, data = result });
        }
        
    }
}
