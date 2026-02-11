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
    // Request: Gửi lên SessionId (Guid)
    public record GetPracticeSessionQuery(Guid SessionId) : IRequest<PracticeSessionDto>;

    public class GetPracticeSessionQueryHandler : IRequestHandler<GetPracticeSessionQuery, PracticeSessionDto>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public GetPracticeSessionQueryHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PracticeSessionDto> Handle(GetPracticeSessionQuery request, CancellationToken cancellationToken)
        {
            // 1. Lấy thông tin PracticeAttempt và các câu trả lời đã lưu
            var attempt = await _context.PracticeAttempts
                .Include(a => a.Answers) // Quan trọng: Load câu trả lời cũ để hiển thị lại
                .Include(a => a.PartResults)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.SessionId, cancellationToken);

            if (attempt == null)
                throw new KeyNotFoundException($"Không tìm thấy phiên làm bài với ID {request.SessionId}");

            if (attempt.Status == AttemptStatus.Submitted)
                throw new InvalidOperationException("Bài tập này đã hoàn thành, không thể làm tiếp.");

            // 2. Lấy danh sách câu hỏi dựa trên các QuestionId đã lưu trong bảng PracticeAnswers
            //    (Phải lấy theo đúng thứ tự đã lưu trong PracticeAnswers)
            var answerData = attempt.Answers.OrderBy(a => a.OrderIndex).ToList();
            var questionIds = answerData.Select(a => a.QuestionId).ToList();

            // Load Question Entities từ DB
            var questions = await _context.Questions
                .Where(q => questionIds.Contains(q.Id))
                .Include(q => q.Answers)
                .Include(q => q.Media)
                .Include(q => q.Group).ThenInclude(g => g.Media)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // 3. Khởi tạo Session DTO
            var session = new PracticeSessionDto
            {
                SessionId = attempt.Id,
                Title = attempt.Title,
                TotalQuestions = attempt.TotalQuestions,
                // Tính thời gian còn lại (nếu có giới hạn)
                Duration = attempt.TimeLimitSeconds.HasValue
                    ? attempt.TimeLimitSeconds.Value / 60
                    : 0,
                Parts = new List<PracticePartDto>()
            };

            // 4. Tái tạo cấu trúc Parts và Questions
            //    (Ở đây tôi giả định gom hết vào 1 part hoặc bạn phải lưu PartId trong PracticeAnswers nếu muốn chia part chính xác như lúc start)

            // Cách đơn giản: Map lại câu hỏi và điền đáp án người dùng đã chọn
            var questionDtos = new List<PracticeQuestionDto>();

            foreach (var storedAnswer in answerData)
            {
                var questionEntity = questions.FirstOrDefault(q => q.Id == storedAnswer.QuestionId);
                if (questionEntity == null) continue;

                var qDto = _mapper.Map<PracticeQuestionDto>(questionEntity);

                // --- QUAN TRỌNG: Restore trạng thái cũ ---
                qDto.OrderIndex = storedAnswer.OrderIndex;
                qDto.QuestionNumber = storedAnswer.OrderIndex;

                // Nếu bạn muốn Frontend hiển thị đáp án đã chọn, 
                // bạn cần thêm field 'SelectedAnswerId' vào PracticeQuestionDto (xem mục 3 bên dưới)
                qDto.SelectedAnswerId = storedAnswer.SelectedAnswerId;
                qDto.IsMarkedForReview = storedAnswer.IsMarkedForReview;

                questionDtos.Add(qDto);
            }

            // Gom vào 1 Part giả định (hoặc logic chia part của bạn)
            session.Parts.Add(new PracticePartDto
            {
                PartId = attempt.CategoryId ?? Guid.Empty,
                PartName = "Resume Part",
                Questions = questionDtos
            });

            return session;
        }
    }
}
