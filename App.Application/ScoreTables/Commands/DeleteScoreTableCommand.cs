using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.ScoreTables.Commands
{
    // ============================================================
    // COMMAND 5: DELETE ScoreTable (soft delete)
    // DELETE /api/score-tables/{id}
    // ============================================================
    public class DeleteScoreTableCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }

    public class DeleteScoreTableCommandHandler
        : IRequestHandler<DeleteScoreTableCommand, bool>
    {
        private readonly IAppDbContext _context;

        public DeleteScoreTableCommandHandler(IAppDbContext context)
            => _context = context;

        public async Task<bool> Handle(
            DeleteScoreTableCommand request,
            CancellationToken ct)
        {
            var entity = await _context.ScoreTables
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy ScoreTable với Id = {request.Id}");

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
