using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Share;
using App.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;


namespace App.Application.Categories.Queries
{
    // GET DANH SÁCH DANH MỤC
    public record GetCategoriesQuery : BaseGetAllQuery<CategoryDto>
    {
        public string? CodeType { get; set; }
        public Guid? ParentId { get; set; }
        public bool? IsActive { get; set; }
    }


    public class GetCategoryHandler : BaseQueryHandler<GetCategoriesQuery, Category, CategoryDto>
    {
        public GetCategoryHandler(IAppDbContext context, IMapper mapper) : base(context, mapper)
        {
        }

        // Ghi đè hàm BuildQuery để viết logic lọc riêng
        protected override IQueryable<Category> BuildQuery(IQueryable<Category> query, GetCategoriesQuery request)
        {
            query = query.Where(x => !x.IsDeleted);
            // 1. Logic Search Keyword (Tìm theo tên hoặc mã)


            if (!string.IsNullOrEmpty(request.CodeType))
            {
                query = query.Where(x => x.CodeType == request.CodeType);
            }
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var key = request.Keyword.Trim();
                query = query.Where(x =>
                    EF.Functions.Like(x.Name, $"%{key}%") ||
                    (x.Code != null && EF.Functions.Like(x.Code, $"%{key}%"))
                );
            }

            query = query.Where(x => x.ParentId == request.ParentId);
           
            // 3. Logic Filter Status
            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == request.IsActive);
            }

            if (request.CreateFrom.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= request.CreateFrom.Value);
            }

            if (request.CreateTo.HasValue)
            {
                var toDate = request.CreateTo.Value
                    .Date
                    .AddDays(1)
                    .AddTicks(-1);

                query = query.Where(x => x.CreatedAt <= toDate);
            }

            query = query
                .Include(x => x.Children)
                 .ThenInclude(x => x.Children)
              .OrderBy(x => x.OrderIndex)
              .ThenBy(x => x.CodeType)
              .ThenBy(x => x.Name);

            return query; // Trả về query đã được lọc
        }
    }
}
