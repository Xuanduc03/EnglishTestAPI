using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace App.Application.Exams.Commands
{
    public class AddQuestionsToSectionCommand : IRequest<List<Guid>>
    {
        public Guid ExamId { get; set; }
        public Guid SectionId { get; set; }
        public Guid CategoryId { get; set; }
        public List<Guid> QuestionIds { get; set; } = new();
        public decimal DefaultPoint { get; set; } = 1.0m;
    }

    public class AddQuestionToSectionCommandHandler : IRequestHandler<AddQuestionsToSectionCommand, List<Guid>>
    {
        private readonly IAppDbContext _context;

        public AddQuestionToSectionCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Guid>> Handle(AddQuestionsToSectionCommand request, CancellationToken cancellation)
        {
            if (request.QuestionIds == null || !request.QuestionIds.Any())
                throw new ValidationException("Danh sách câu hỏi không được để trống");

            var section = await _context.ExamSections
                .Include(s => s.Exam)
                .Include(c => c.Category)
                .FirstOrDefaultAsync(x =>
                    x.Id == request.SectionId &&
                    x.ExamId == request.ExamId &&
                    !x.IsDeleted, cancellation)
                ?? throw new ValidationException("Section không tồn tại hoặc không thuộc Exam");

            // ── 1. Load câu hỏi đã có trong section 
            var existedQuestionIds = await _context.ExamQuestions
                .Where(x => x.ExamSectionId == request.SectionId)
                .Select(x => x.QuestionId)
                .ToListAsync(cancellation);

            // ── 2. Expand group → câu hỏi con 
            var expandedIds = new List<Guid>();

            foreach (var id in request.QuestionIds)
            {
                var isGroup = await _context.QuestionGroups
                    .AnyAsync(g => g.Id == id && !g.IsDeleted, cancellation);

                if (isGroup)
                {
                    var childIds = await _context.Questions
                        .Where(q => q.GroupId == id && !q.IsDeleted)
                        .OrderBy(q => q.OrderIndex)
                        .Select(q => q.Id)
                        .ToListAsync(cancellation);

                    expandedIds.AddRange(childIds);
                }
                else
                {
                    expandedIds.Add(id);
                }
            }

            // ── 3. Lọc trùng 
            var newQuestionIds = expandedIds
                .Where(id => !existedQuestionIds.Contains(id))
                .Distinct()
                .ToList();

            if (!newQuestionIds.Any())
                throw new ValidationException("Tất cả câu hỏi đã tồn tại trong phần thi");

            // ── 4. Validate limit (dùng count đã expand) 
            var currentCount = await _context.ExamQuestions
                .Where(x => x.ExamSectionId == request.SectionId)
                .CountAsync(cancellation);

            ValidateQuestionLimit(section, currentCount + newQuestionIds.Count);

            // ── 5. Insert 
            var nextIndex = currentCount;
            var nextNo = currentCount + 1;
            var newExamQuestions = new List<ExamQuestion>();

            foreach (var qId in newQuestionIds)
            {
                newExamQuestions.Add(new ExamQuestion
                {
                    Id = Guid.NewGuid(),
                    ExamId = request.ExamId,
                    ExamSectionId = request.SectionId,
                    QuestionId = qId,
                    Point = request.DefaultPoint,
                    OrderIndex = nextIndex++,
                    QuestionNo = nextNo++,
                    IsMandatory = true,
                    IsShuffleable = true,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                });
            }

            await _context.ExamQuestions.AddRangeAsync(newExamQuestions, cancellation);
            await _context.SaveChangesAsync(cancellation);

            return newExamQuestions.Select(x => x.Id).ToList();
        }
        private void ValidateQuestionLimit(ExamSection section, int totalAfterAdd)
        {
            if (section.Exam.Type != ExamType.TOEIC) return;
            if (section.Category == null)
                throw new ValidationException("Section chưa có category");

            var limits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Part 1", 6  },
                { "Part 2", 25 },
                { "Part 3", 39 },
                { "Part 4", 30 },
                { "Part 5", 30 },
                { "Part 6", 16 },
                { "Part 7", 54 },
            };

            var partKey = limits.ContainsKey(section.Category.Code ?? "")
                ? section.Category.Code
                : limits.ContainsKey(section.Category.Name ?? "")
                    ? section.Category.Name
                    : null;

            if (partKey == null) return;

            var max = limits[partKey];
            if (totalAfterAdd > max)
                throw new ValidationException(
                    $"{partKey} chỉ được tối đa {max} câu. Hiện tại sẽ là {totalAfterAdd} câu sau khi thêm.");
        }
    }
}