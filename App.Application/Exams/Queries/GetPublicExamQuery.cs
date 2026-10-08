using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Exams.Queries;

public record GetPublicExamQuery(Guid ExamId) : IRequest<PublicExamDto>;

public record PublicExamDto(
    Guid Id,
    string Code,
    string Title,
    string? Description,
    ExamType Type,
    ExamCategory Category,
    ExamLevel Level,
    int Duration,
    int QuestionCount);

public class GetPublicExamQueryHandler(IAppDbContext context)
    : IRequestHandler<GetPublicExamQuery, PublicExamDto>
{
    public async Task<PublicExamDto> Handle(GetPublicExamQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await context.Exams.AsNoTracking()
            .Where(e => e.Id == request.ExamId && !e.IsDeleted && e.IsActive
                && e.Status == ExamStatus.Published
                && (!e.StartDate.HasValue || e.StartDate <= now)
                && (!e.EndDate.HasValue || e.EndDate >= now))
            .Select(e => new PublicExamDto(e.Id, e.Code, e.Title, e.Description,
                e.Type, e.Category, e.Level, e.Duration,
                e.Sections.Where(s => !s.IsDeleted)
                    .SelectMany(s => s.ExamQuestions.Where(q => !q.IsDeleted)).Count()))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đề thi đã xuất bản");
    }
}
