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
            if (section.Category == null)
                throw new ValidationException("Section chưa có category để phân loại.");

            var categoryCode = (section.Category.Code ?? "").ToUpperInvariant();
            var categoryName = (section.Category.Name ?? "").ToUpperInvariant();

            if (section.Exam.Type == ExamType.TOEIC)
            {
                var toeicLimits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "PART 1", 6  },
            { "PART 2", 25 },
            { "PART 3", 39 },
            { "PART 4", 30 },
            { "PART 5", 30 },
            { "PART 6", 16 },
            { "PART 7", 54 },
        };

                // Match theo Name vì Code TOEIC thường là TOEIC_L_P1 etc.
                var matchedKey = toeicLimits.Keys.FirstOrDefault(k =>
                    categoryName.Contains(k.ToUpperInvariant()) ||
                    categoryCode.Contains(k.Replace(" ", "").ToUpperInvariant()));

                if (matchedKey != null)
                {
                    var max = toeicLimits[matchedKey];
                    if (totalAfterAdd > max)
                        throw new ValidationException(
                            $"TOEIC {matchedKey} chỉ được tối đa {max} câu. " +
                            $"Hiện tại sẽ là {totalAfterAdd} câu sau khi thêm.");
                }
            }
            else if (section.Exam.Type == ExamType.IELTS)
            {
                // Map theo Code convention: IELTS_L_S1, IELTS_L_S2, IELTS_R_P1...
                var ieltsLimits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            // Code-based
            { "IELTS_L_S1", 10 },
            { "IELTS_L_S2", 10 },
            { "IELTS_L_S3", 10 },
            { "IELTS_L_S4", 10 },
            { "IELTS_R_P1", 14 },
            { "IELTS_R_P2", 14 },
            { "IELTS_R_P3", 14 },

            // Name-based fallback
            { "SECTION 1", 10 },
            { "SECTION 2", 10 },
            { "SECTION 3", 10 },
            { "SECTION 4", 10 },
            { "PASSAGE 1", 14 },
            { "PASSAGE 2", 14 },
            { "PASSAGE 3", 14 },
        };

                // Ưu tiên match Code trước, fallback Name
                var matchedKey = ieltsLimits.Keys.FirstOrDefault(k =>
                    categoryCode == k.ToUpperInvariant())
                    ?? ieltsLimits.Keys.FirstOrDefault(k =>
                    categoryName == k.ToUpperInvariant());

                if (matchedKey != null)
                {
                    var max = ieltsLimits[matchedKey];
                    if (totalAfterAdd > max)
                        throw new ValidationException(
                            $"IELTS {section.Category.Name} chỉ được tối đa {max} câu. " +
                            $"Hiện tại sẽ là {totalAfterAdd} câu sau khi thêm.");
                }
            }
        }
    }
}