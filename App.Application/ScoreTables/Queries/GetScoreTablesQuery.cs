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
    // GET /api/score-tables?skillCategoryId=...&keyword=...
    // ============================================================
    public record GetScoreTablesQuery : BaseGetAllQuery<ScoreTableListDto>
    {
        // Filter theo Skill: LISTENING hoặc READING
        public Guid? SkillCategoryId { get; init; }
        public string? Keyword { get; init; }
    }

    public class GetScoreTablesQueryHandler
        : BaseQueryHandler<GetScoreTablesQuery, ScoreTable, ScoreTableListDto>
    {
        public GetScoreTablesQueryHandler(IAppDbContext context, IMapper mapper)
            : base(context, mapper) { }

        protected override IQueryable<ScoreTable> BuildQuery(
            IQueryable<ScoreTable> query, GetScoreTablesQuery request)
        {
            query = query
                .Include(s => s.SkillCategory)  // Lấy tên Skill (LISTENING/READING)
                .Include(s => s.Entries)         // Lấy danh sách quy đổi
                .Where(s => !s.IsDeleted);

            // Filter theo Skill
            if (request.SkillCategoryId.HasValue)
                query = query.Where(s => s.SkillCategoryId == request.SkillCategoryId.Value);

            // Filter theo IsActive
            query = query.Where(s => s.IsActive);

            // Tìm theo tên bảng hoặc tên Skill
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var key = request.Keyword.Trim();
                query = query.Where(s =>
                    EF.Functions.Like(s.Name, $"%{key}%") ||
                    EF.Functions.Like(s.SkillCategory.Name, $"%{key}%")
                );
            }

            query = query.OrderByDescending(s => s.CreatedAt);
            return query;
        }
    }
}