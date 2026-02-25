using App.Application.DTOs.Questions;
using App.Application.Interfaces;
using App.Application.Share;
using App.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Questions.Queries
{
    // 1. DTO Query: Các tham số Frontend gửi lên
    public record GetAllQuestionsQuery : BaseGetAllQuery<QuestionListDto>
    {
        public Guid? CategoryId { get; set; } // Quan trọng nhất: Để lọc theo Part
        public string? QuestionType { get; set; } // SingleChoice, Essay...
        public Guid? DifficultyId { get; set; } // Thêm: Lọc theo độ khó
        public bool? IsActive { get; set; } // Thêm: Lọc theo trạng thái
        public string? SortBy { get; set; } // Thêm: Trường sắp xếp
        public string? SortOrder { get; set; } // Thêm: Thứ tự sắp xếp
        public DateTime? CreateFrom { get; set; }
        public DateTime? CreateTo { get; set; }
    }

    // 2. Handler: Xử lý logic
    public class GetQuestionsHandler : BaseQueryHandler<GetAllQuestionsQuery, Question, QuestionListDto>
    {
        public GetQuestionsHandler(IAppDbContext context, IMapper mapper) : base(context, mapper)
        {
        }

        protected override IQueryable<Question> BuildQuery(IQueryable<Question> query, GetAllQuestionsQuery request)
        {
            // Luôn lọc soft delete
            query = query
                .AsNoTracking()
                .Include(q => q.Group)
                .Where(x => !x.IsDeleted);

            // 1. LOGIC QUAN TRỌNG NHẤT: Lọc theo Danh mục (Part)
            // Phục vụ cho cái bảng con (Detail Table) ở Frontend
            if (request.CategoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId == request.CategoryId);
            }
            // 2. Lọc theo độ khó 
            if (request.DifficultyId.HasValue)
            {
                query = query.Where(x => x.DifficultyId == request.DifficultyId);
            }

            // 3. Lọc theo trạng thái 
            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == request.IsActive.Value);
            }


            // 2. Logic tìm kiếm (Search Keyword)
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var key = request.Keyword.Trim();
                query = query.Where(x =>
                     EF.Functions.Like(x.Content, $"%{key}%") ||
                     EF.Functions.Like(x.Explanation, $"%{key}%") ||
                     (x.Group != null && EF.Functions.Like(x.Group.Content, $"%{key}%")) 
                 );
            }

            // 5. Lọc theo loại câu hỏi
            if (!string.IsNullOrEmpty(request.QuestionType))
            {
                query = query.Where(x => x.QuestionType == request.QuestionType);
            }

            // 4. Lọc ngày tháng
            if (request.CreateFrom.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= request.CreateFrom.Value);
            }
            if (request.CreateTo.HasValue)
            {
                var toDate = request.CreateTo.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.CreatedAt <= toDate);
            }
            query = ApplySorting(query, request);


            return query;
        }


        private IQueryable<Question> ApplySorting(IQueryable<Question> query, GetAllQuestionsQuery request)
        {
            if (string.IsNullOrWhiteSpace(request.SortBy))
            {
                return query.OrderByDescending(x => x.CreatedAt);
            }

            var isDescending = (request.SortOrder?.ToLower() ?? "desc") == "desc";

            return request.SortBy.ToLower() switch
            {
                "id" => isDescending
                    ? query.OrderByDescending(q => q.Id)
                    : query.OrderBy(q => q.Id),

                "content" => isDescending
                    ? query.OrderByDescending(x => x.Content)
                    : query.OrderBy(x => x.Content),

                "difficulty" => isDescending
                    ? query.OrderByDescending(x => x.Difficulty.Name)
                    : query.OrderBy(x => x.Difficulty.Name),

                "createAt" => isDescending
                    ? query.OrderByDescending(x => x.CreatedAt)
                    : query.OrderBy(x => x.CreatedAt),

                "updatedat" => isDescending
                    ? query.OrderByDescending(x => x.UpdatedAt)
                    : query.OrderBy(x => x.UpdatedAt),

                _ => query.OrderByDescending(x => x.CreatedAt)


            };
        }
    }

}
