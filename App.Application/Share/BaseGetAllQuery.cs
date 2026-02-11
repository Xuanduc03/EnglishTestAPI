using MediatR;
using AutoMapper;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using AutoMapper.QueryableExtensions;
using App.Application.Interfaces;
using App.Domain.Shares;


namespace App.Application.Share
{
    // 1. INPUT CHUNG (Frontend gửi lên)
    public abstract record BaseGetAllQuery<TDto> : IRequest<PaginatedResult<TDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Keyword { get; set; } // Search chung

        // Sort: Key là tên cột, Value là "asc" hoặc "desc"
        // VD: { "createdAt": "desc" }
        public Dictionary<string, string>? Sort { get; set; }
        public DateTime? CreateFrom { get; set; }
        public DateTime? CreateTo { get; set; }
    }

    // 2. OUTPUT CHUNG (Trả về cho Frontend)
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; }
        public MetaData Meta { get; set; }

        public PaginatedResult(List<T> items, int total, int page, int pageSize)
        {
            Items = items;
            Meta = new MetaData(page, pageSize, total);
        }
    }

    public class MetaData
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
         
        public MetaData(int page, int pageSize, int total)
        {
            Page = page;
            PageSize = pageSize;
            Total = total;
        }
    }

    public abstract class BaseQueryHandler<TRequest, TEntity, TDto>
        : IRequestHandler<TRequest, PaginatedResult<TDto>>
        where TRequest : BaseGetAllQuery<TDto>  // ✅ Now TRequest automatically implements IRequest<PaginatedResult<TDto>>
        where TEntity : class
    {
        protected readonly IAppDbContext _context;
        protected readonly IMapper _mapper;
        // Constructor bắt buộc phải có
        protected BaseQueryHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<PaginatedResult<TDto>> Handle(TRequest request, CancellationToken cancellation)
        {
            // 1. Khởi tạo Query (NoTracking cho nhẹ)
            var query = _context.Set<TEntity>().AsNoTracking().AsQueryable();

            // 2. Xử lý Xóa mềm (Thay thế đoạn Reflection của sếp bạn)
            // Nếu TEntity có kế thừa ISoftDelete -> Tự động lọc bản ghi chưa xóa
            if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
            {
                // Ép kiểu về ISoftDelete để lọc
                query = query.Where(e => ((ISoftDelete)e).IsDeleted == false);
            }

            // 3. Gọi hàm ảo để lớp con chèn logic Filter riêng (QUAN TRỌNG)
            query = BuildQuery(query, request);

            // 4. Xử lý Sắp xếp động (Dynamic LINQ)
            query = ApplySorting(query, request.Sort);


            // 5. Đếm tổng số bản ghi
            int total = await query.CountAsync(cancellation);

            // 6. Phân trang & Map sang DTO
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<TDto>(_mapper.ConfigurationProvider) // AutoMapper
                .ToListAsync(cancellation);

            return new PaginatedResult<TDto>(items, total, request.Page, request.PageSize);                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              
        }

        protected abstract IQueryable<TEntity> BuildQuery(IQueryable<TEntity> query, TRequest request);

        // Hàm sắp xếp chung (Có thể dùng luôn, không cần sửa)
        protected virtual IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, Dictionary<string, string>? sort)
        {
            if (sort == null || !sort.Any())
            {
                return query;
            }


            string orderByString = "";
            foreach (var item in sort)
            {
                string direction = item.Value.ToLower() == "asc" ? "ascending" : "descending";

                string propertyName = char.ToUpper(item.Key[0]) + item.Key.Substring(1);

                orderByString += $"{propertyName} {direction},";
            }

            // Xóa dấu phẩy cuối & áp dụng
            return query.OrderBy(orderByString.TrimEnd(','));
        }

    
    }
}
