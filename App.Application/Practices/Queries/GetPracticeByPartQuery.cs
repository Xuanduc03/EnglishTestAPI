using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Practices.Queries
{
    /// <summary>
    /// Query: Lấy câu hỏi practice theo Part (Category)
    /// Use case: User chọn Part 1, Part 2... để luyện tập
    /// </summary>
    public record GetPracticeByPartQuery(
        List<Guid> CategoryIds,        // Part IDs (có thể chọn nhiều part)
        int QuestionsPerPart = 10,     // Số câu mỗi part
        bool RandomOrder = true         // Random thứ tự câu hỏi
    ) : IRequest<PracticeSessionDto>;

    public class GetPracticeByPartQueryHandler : IRequestHandler<GetPracticeByPartQuery, PracticeSessionDto>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public GetPracticeByPartQueryHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PracticeSessionDto> Handle(GetPracticeByPartQuery request, CancellationToken cancellationToken)
        {
            var session = new PracticeSessionDto
            {
                SessionId = Guid.NewGuid(),
                Title = "Practice Session",
                TotalQuestions = 0,
                Duration = CalculateDuration(request.CategoryIds, request.QuestionsPerPart),
                Parts = new List<PracticePartDto>()
            };

            int globalQuestionNumber = 1; // Question number tổng thể (1-200)

            // Duyệt qua từng Part được chọn
            foreach (var categoryId in request.CategoryIds)
            {
                var category = await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

                if (category == null) continue;

                // Tạo PartDto
                var partDto = new PracticePartDto
                {
                    PartId = category.Id,
                    PartName = category.Name,
                    PartNumber = ExtractPartNumber(category.Name),
                    PartDescription = category.Description,
                    Questions = new List<PracticeQuestionDto>()
                };

                // ============================================
                // LẤY CÂU HỎI THEO PART
                // ============================================

                var questions = await GetQuestionsByPart(
                    categoryId,
                    request.QuestionsPerPart,
                    request.RandomOrder,
                    cancellationToken
                );

                // ============================================
                // XỬ LÝ CÂU HỎI (SINGLE VS GROUP)
                // ============================================

                var processedQuestions = await ProcessQuestions(
                    questions,
                    globalQuestionNumber,
                    cancellationToken
                );

                partDto.Questions = processedQuestions;
                session.Parts.Add(partDto);

                // Update global question number
                globalQuestionNumber += processedQuestions.Count;
                session.TotalQuestions += processedQuestions.Count;
            }

            return session;
        }

        // ============================================
        // HELPER: LẤY CÂU HỎI THEO PART
        // ============================================

        private async Task<List<Question>> GetQuestionsByPart(
            Guid categoryId,
            int count,
            bool randomOrder,
            CancellationToken cancellationToken)
        {
            var query = _context.Questions
                .AsNoTracking()
                .Where(q => q.CategoryId == categoryId)
                .Include(q => q.Media)
                .Include(q => q.Answers)
                .Include(q => q.Group)
                    .ThenInclude(g => g.Media)
                .Include(q => q.Category)
                .Include(q => q.Difficulty);

            // ============================================
            // XỬ LÝ GROUP QUESTIONS
            // ============================================

            // Lấy tất cả questions (bao gồm cả trong group)
            var allQuestions = await query.ToListAsync(cancellationToken);

            // Phân loại: Single vs Group
            var singleQuestions = allQuestions.Where(q => q.GroupId == null).ToList();
            var groupQuestions = allQuestions.Where(q => q.GroupId != null).ToList();

            var selectedQuestions = new List<Question>();

            if (groupQuestions.Any())
            {
                // ============================================
                // PART CÓ GROUP (Part 3, 4, 6, 7)
                // ============================================

                // Lấy danh sách groups
                var groupIds = groupQuestions.Select(q => q.GroupId!.Value).Distinct().ToList();

                if (randomOrder)
                {
                    // Random groups
                    groupIds = groupIds.OrderBy(_ => Guid.NewGuid()).ToList();
                }

                // Tính số group cần lấy
                int questionsPerGroup = GetQuestionsPerGroup(categoryId);
                int groupsNeeded = (int)Math.Ceiling((double)count / questionsPerGroup);

                // Lấy questions từ các groups đã chọn
                var selectedGroupIds = groupIds.Take(groupsNeeded).ToList();
                selectedQuestions = groupQuestions
                    .Where(q => selectedGroupIds.Contains(q.GroupId!.Value))
                    .OrderBy(q => q.GroupId)
                    .Take(count)
                    .ToList();
            }
            else
            {
                // ============================================
                // PART KHÔNG CÓ GROUP (Part 1, 2, 5)
                // ============================================

                if (randomOrder)
                {
                    selectedQuestions = singleQuestions
                        .OrderBy(_ => Guid.NewGuid())
                        .Take(count)
                        .ToList();
                }
                else
                {
                    selectedQuestions = singleQuestions
                        .Take(count)
                        .ToList();
                }
            }

            return selectedQuestions;
        }

        // ============================================
        // HELPER: XỬ LÝ VÀ MAP QUESTIONS
        // ============================================

        private async Task<List<PracticeQuestionDto>> ProcessQuestions(
            List<Question> questions,
            int startQuestionNumber,
            CancellationToken cancellationToken)
        {
            var result = new List<PracticeQuestionDto>();
            var processedGroups = new HashSet<Guid>();
            int currentQuestionNumber = startQuestionNumber;

            foreach (var question in questions)
            {
                var dto = _mapper.Map<PracticeQuestionDto>(question);
                dto.QuestionNumber = currentQuestionNumber;
                dto.OrderIndex = currentQuestionNumber - startQuestionNumber + 1;

                // ============================================
                // XỬ LÝ GROUP QUESTION
                // ============================================

                if (question.GroupId.HasValue && !processedGroups.Contains(question.GroupId.Value))
                {
                    // Load group info (chỉ load 1 lần cho mỗi group)
                    var group = question.Group;

                    if (group != null)
                    {
                        dto.GroupContent = group.Content;
                        dto.GroupMedia = _mapper.Map<List<PracticeMediaDto>>(group.Media);

                        // Đếm số câu trong group này
                        var questionsInGroup = questions
                            .Where(q => q.GroupId == question.GroupId)
                            .ToList();

                        dto.TotalQuestionsInGroup = questionsInGroup.Count;

                       
                        processedGroups.Add(question.GroupId.Value);
                    }
                }

                // Tính vị trí câu hỏi trong group
                if (question.GroupId.HasValue)
                {
                    var questionsInGroup = questions
                        .Where(q => q.GroupId == question.GroupId)
                        .ToList();

                    dto.QuestionIndexInGroup = questionsInGroup.IndexOf(question) + 1;
                }

                result.Add(dto);
                currentQuestionNumber++;
            }

            return result;
        }

        // ============================================
        // HELPER: TÍNH THỜI GIAN
        // ============================================

        private int CalculateDuration(List<Guid> categoryIds, int questionsPerPart)
        {
            // Thời gian mỗi part TOEIC (phút/câu)
            var timePerQuestion = new Dictionary<int, double>
            {
                { 1, 0.5 },   // Part 1: 30s/câu
                { 2, 0.5 },   // Part 2: 30s/câu  
                { 3, 1.5 },   // Part 3: 1.5 phút/câu (có audio)
                { 4, 1.5 },   // Part 4: 1.5 phút/câu (có audio)
                { 5, 0.5 },   // Part 5: 30s/câu
                { 6, 1.0 },   // Part 6: 1 phút/câu
                { 7, 1.5 }    // Part 7: 1.5 phút/câu
            };

            int totalMinutes = 0;
            foreach (var categoryId in categoryIds)
            {
                var partNumber = ExtractPartNumberFromId(categoryId);
                if (timePerQuestion.ContainsKey(partNumber))
                {
                    totalMinutes += (int)(questionsPerPart * timePerQuestion[partNumber]);
                }
            }

            return totalMinutes > 0 ? totalMinutes : questionsPerPart; // Default 1 phút/câu
        }

        // ============================================
        // HELPER: SỐ CÂU MỖI GROUP
        // ============================================

        private int GetQuestionsPerGroup(Guid categoryId)
        {
            var partNumber = ExtractPartNumberFromId(categoryId);

            return partNumber switch
            {
                3 => 3,  // Part 3: 3 câu/conversation
                4 => 3,  // Part 4: 3 câu/talk
                6 => 4,  // Part 6: 4 câu/passage
                7 => 3,  // Part 7: Average 3 câu/passage (có thể 2-5)
                _ => 1
            };
        }

        // ============================================
        // HELPER: EXTRACT PART NUMBER
        // ============================================

        private int ExtractPartNumber(string partName)
        {
            if (string.IsNullOrEmpty(partName)) return 0;

            var parts = partName.Split(' ');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int number))
            {
                return number;
            }

            return 0;
        }

        private int ExtractPartNumberFromId(Guid categoryId)
        {
            var category = _context.Categories
                .AsNoTracking()
                .FirstOrDefault(c => c.Id == categoryId);

            return category != null ? ExtractPartNumber(category.Name) : 0;
        }
    }

}