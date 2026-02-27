
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace App.Application.Exams.Commands
{
    // B3: Thêm câu hỏi vào section ( thêm câu hỏi vào phần thi)
    // Input: SectionId, QuestionIds[], Points[], OrderIndexes[]
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
            // validate input 
            if(request.QuestionIds == null || !request.QuestionIds.Any())
            {
                throw new ValidationException("Danh sách câu hỏi không được để trống");
            }

            var section = await _context.ExamSections
                .Include(s => s.Exam)
                .Include(c => c.Category)
                .FirstOrDefaultAsync(x =>
                    x.Id == request.SectionId &&
                    x.ExamId == request.ExamId &&
                    !x.IsDeleted,
                    cancellation);

            if (section == null)
                throw new ValidationException("Section không tồn tại hoặc không thuộc Exam");
           

            var existedQuestionIds = await _context.ExamQuestions
                .Where(x => x.ExamSectionId == request.SectionId)
                .Select(x => x.QuestionId)
                .ToListAsync(cancellation);

            var newQuestionIds = request.QuestionIds
                .Where(id => !existedQuestionIds.Contains(id))
                .ToList();

            if (!newQuestionIds.Any())
            {
                throw new ValidationException("Tất cả câu hỏi đã tồn tại trong phần thi");
            }

            // calculate next question
            var currentMaxIndex = await _context.ExamQuestions
                .AsNoTracking()
                .Where(x => x.ExamSectionId == request.SectionId)
                .Select(x => (int?)x.OrderIndex)
                .MaxAsync(cancellation) ?? -1;

            var currentQuestionNo = await _context.ExamQuestions
                .Where(x => x.ExamSectionId == request.SectionId)
                .CountAsync(cancellation);

            var totalAfterAdd = currentQuestionNo + newQuestionIds.Count;
            ValidateQuestionLimit(section, totalAfterAdd);

            // create list entity
            var newExamQuestions = new List<ExamQuestion>();
            var nextIndex = currentMaxIndex + 1;
            var nextNo = currentQuestionNo + 1;

            foreach( var qId in newQuestionIds)
            {
                var examQuestion = new ExamQuestion
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
                    CreatedAt = DateTime.Now,
                    IsDeleted = false
                };

                newExamQuestions.Add(examQuestion);
            }

            //  save db
            await _context.ExamQuestions.AddRangeAsync(newExamQuestions, cancellation);
            await _context.SaveChangesAsync(cancellation);

            // return list id
            return newExamQuestions.Select(x => x.Id).ToList();
        }


        private void ValidateQuestionLimit(ExamSection section, int totalAfterAdd)
        {
            if (section.Exam.Type != ExamType.TOEIC)
                return;

            if (section.Category == null)
                throw new ValidationException("Section chưa có category");

            var limits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Part 1", 6 },
                { "Part 2", 25 },
                { "Part 3", 39 },
                { "Part 4", 30 },
                { "Part 5", 30 },
                { "Part 6", 16 },
                { "Part 7", 54 }
            };

            var partName = section.Category.Code;

            if (!limits.ContainsKey(partName))
                return;

            var max = limits[partName];

            if (totalAfterAdd > max)
                throw new ValidationException(
                    $"{partName} chỉ được tối đa {max} câu. Sau khi thêm sẽ là {totalAfterAdd} câu.");
        }
    }

}
