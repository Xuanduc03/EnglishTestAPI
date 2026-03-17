using App.Application.DTOs.Questions;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Questions.Queries
{
    /// <summary>
    /// Query : build tree để xử lý hiển thị câu hỏi nhóm 
    /// </summary>
    public class GetQuestionsHierarchyQuery : IRequest<PaginatedHierarchyResult>
    {
        public Guid? CategoryId { get; set; }
        public string? Keyword { get; set; }
        public Guid? DifficultyId { get; set; }
        public bool? IsActive { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PaginatedHierarchyResult
    {
        public List<QuestionHierarchyItemDto> Items { get; set; } = new();
        public int TotalCount { get; set; }   // Tổng số item (group + single)
        public int TotalQuestions { get; set; } // Tổng số câu hỏi thực tế
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class GetQuestionsHierarchyQueryHandler
        : IRequestHandler<GetQuestionsHierarchyQuery, PaginatedHierarchyResult>
    {
        private readonly IAppDbContext _context;

        public GetQuestionsHierarchyQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedHierarchyResult> Handle(
            GetQuestionsHierarchyQuery request,
            CancellationToken cancellationToken)
        {
            // ── PHẦN 1: Câu hỏi NHÓM 
            var groupQuery = _context.QuestionGroups
                .AsNoTracking()
                .Where(g => !g.IsDeleted);

            if (request.CategoryId.HasValue)
                groupQuery = groupQuery.Where(g => g.CategoryId == request.CategoryId);

            if (request.IsActive.HasValue)
                groupQuery = groupQuery.Where(g => g.IsActive == request.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var kw = request.Keyword.Trim();
                groupQuery = groupQuery.Where(g =>
                    EF.Functions.Like(g.Content, $"%{kw}%"));
            }

            var groups = await groupQuery
                .Include(g => g.Category)
                .Include(g => g.Media)
                .Include(g => g.Questions.Where(q => !q.IsDeleted))
                    .ThenInclude(q => q.Answers)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync(cancellationToken);

            // ── PHẦN 2: Câu hỏi ĐƠN (không thuộc group) 
            var singleQuery = _context.Questions
                .AsNoTracking()
                .Where(q => !q.IsDeleted && q.GroupId == null);

            if (request.CategoryId.HasValue)
                singleQuery = singleQuery.Where(q => q.CategoryId == request.CategoryId);

            if (request.IsActive.HasValue)
                singleQuery = singleQuery.Where(q => q.IsActive == request.IsActive.Value);

            if (request.DifficultyId.HasValue)
                singleQuery = singleQuery.Where(q => q.DifficultyId == request.DifficultyId);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var kw = request.Keyword.Trim();
                singleQuery = singleQuery.Where(q =>
                    EF.Functions.Like(q.Content, $"%{kw}%"));
            }

            var singles = await singleQuery
                .Include(q => q.Category)
                .Include(q => q.Difficulty)
                .Include(q => q.Answers)
                .Include(q => q.Tags)
                .Include(q => q.Media)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync(cancellationToken);

            // ── PHẦN 3: Build tree + phân trang 
            var allItems = new List<QuestionHierarchyItemDto>();

            // Groups → mỗi group = 1 item cha
            allItems.AddRange(groups.Select(g => new QuestionHierarchyItemDto
            {
                ItemType = "group",
                CategoryId = g.CategoryId,
                CategoryName = g.Category?.Name,
                GroupId = g.Id,
                GroupContent = g.Content,
                Children = g.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(q => new QuestionChildDto
                    {
                        Id = q.Id,
                        Content = q.Content ?? "",
                        AnswerCount = q.Answers?.Count ?? 0,
                        DifficultyName = "",   
                        QuestionType = q.QuestionType.ToString(),
                        OrderIndex = q.OrderIndex,
                    }).ToList()
            }));

            // Singles → mỗi câu = 1 item
            allItems.AddRange(singles.Select(q => new QuestionHierarchyItemDto
            {
                ItemType = "single",
                CategoryId = q.CategoryId,
                CategoryName = q.Category?.Name,
                Id = q.Id,
                Content = q.Content,
                DifficultyName = q.Difficulty?.Name,
                QuestionType = q.QuestionType.ToString(),
                AnswerCount = q.Answers?.Count ?? 0,
                GroupId = null,
                GroupContent = null,
                Children = null,
            }));

            // Tổng số câu hỏi thực tế (câu con + câu đơn)
            var totalQuestions = groups.Sum(g => g.Questions.Count) + singles.Count;
            var totalCount = allItems.Count;

            // Phân trang in-memory (hoặc chuyển sang DB nếu data lớn)
            var paged = allItems
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PaginatedHierarchyResult
            {
                Items = paged,
                TotalCount = totalCount,
                TotalQuestions = totalQuestions,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
            };
        }
    }
}
