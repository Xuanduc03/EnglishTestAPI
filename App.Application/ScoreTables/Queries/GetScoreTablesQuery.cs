using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Share;
using App.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace App.Application.ScoreTables.Queries
{
    // ============================================================
    // QUERY: Lấy danh sách bảng điểm
    // GET /api/score-tables?examId=...&categoryId=...&keyword=...
    // ============================================================
    public record GetScoreTablesQuery : BaseGetAllQuery<ScoreTableListDto>
    {
        public Guid? ExamId { get; init; }
        public Guid? CategoryId { get; init; } 
        public string? Keyword { get; init; } 
    }

    public class GetScoreTablesQueryHandler
        : BaseQueryHandler<GetScoreTablesQuery, ScoreTable, ScoreTableListDto>
    {
        public GetScoreTablesQueryHandler(IAppDbContext context, IMapper mapper) : base(context, mapper)
        {
        }

        protected override IQueryable<ScoreTable> BuildQuery(IQueryable<ScoreTable> query, GetScoreTablesQuery request)
        {
            // 1. Join bảng để lấy thông tin hiển thị (cho AutoMapper)
            query = query
                .Include(s => s.Exam)
                .Include(s => s.Category) // Quan trọng: Để lấy CategoryName
                .Where(s => !s.IsDeleted);

            // 2. Filter theo ExamId
            if (request.ExamId.HasValue)
            {
                query = query.Where(s => s.ExamId == request.ExamId.Value);
            }

            // 3. Filter theo CategoryId (Thay thế SkillType cũ)
            if (request.CategoryId.HasValue)
            {
                query = query.Where(s => s.CategoryId == request.CategoryId.Value);
            }

            // 4. Filter Keyword (Tìm theo tên đề hoặc tên kỹ năng)
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var key = request.Keyword.Trim();
                query = query.Where(s =>
                    EF.Functions.Like(s.Exam.Title, $"%{key}%") ||
                    EF.Functions.Like(s.Category.Name, $"%{key}%")
                );
            }

            // Mặc định sắp xếp mới nhất lên đầu (nếu chưa có sort)
            query = query.OrderByDescending(s => s.CreatedAt);

            return query;
        }
    }
}