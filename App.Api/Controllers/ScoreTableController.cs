using App.Application.ScoreTables.Commands;
using App.Application.ScoreTables.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    [ApiController]
    [Route("/api/score-table")]
    [Authorize("Admin")]
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

        // ── GET /api/score-tables/{id} ────────────────────────────
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetScoreTableByIdQuery(id));
            return Ok(new { success = true, data = result });
        }

        // ── GET /api/score-tables/by-exam/{examId}/{skillType} ────
        [HttpGet("by-exam/{examId:guid}/{skillType}")]
        public async Task<IActionResult> GetByExam(Guid examId, Guid categoryId)
        {
            var result = await _mediator.Send(
                new GetScoreTableByExamQuery(examId, categoryId));
            return Ok(new { success = true, data = result });
        }

        // ── POST /api/score-tables ────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateScoreTableCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { success = true, data = id });
        }

        // ── PUT /api/score-tables/{id} ────────────────────────────
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateScoreTableCommand command)
        {
            command.Id = id;
            await _mediator.Send(command);
            return Ok(new { success = true, message = "Cập nhật thành công" });
        }

        // ── PATCH /api/score-tables/{id}/rules ───────────────────
        // Thêm hoặc sửa 1 rule
        [HttpPatch("{id:guid}/rules")]
        public async Task<IActionResult> UpsertRule(
            Guid id,
            [FromBody] UpsertConversionRuleCommand command)
        {
            command.ScoreTableId = id;
            await _mediator.Send(command);
            return Ok(new { success = true, message = "Cập nhật rule thành công" });
        }

        // ── DELETE /api/score-tables/{id}/rules/{correctAnswers} ──
        // Xóa 1 rule
        [HttpDelete("{id:guid}/rules/{correctAnswers:int}")]
        public async Task<IActionResult> DeleteRule(Guid id, int correctAnswers)
        {
            await _mediator.Send(new DeleteConversionRuleCommand
            {
                ScoreTableId = id,
                CorrectAnswers = correctAnswers
            });
            return Ok(new { success = true, message = "Xóa rule thành công" });
        }

        // ── DELETE /api/score-tables/{id} ────────────────────────
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteScoreTableCommand { Id = id });
            return Ok(new { success = true, message = "Xóa thành công" });
        }
    }
}
