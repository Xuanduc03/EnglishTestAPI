using App.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using App.Application.Share;
using App.Domain.Entities;
using AutoMapper;
using App.Application.Interfaces;
using App.Domain.Identity;

namespace App.Application.Users.Queries
{

    public record GetUsersQuery : BaseGetAllQuery<UserListDto>
    {
        // filter of user
        public UserRole? Role { get; init; }
        public bool? IsActive { get; init; }
        public string? SortColumn { get; init; }
        public string? SortOrder {  get; init; }
        public bool IncludeDeleted { get; init; } = false;
    }

    public class GetUsersQueryHandler : BaseQueryHandler<GetUsersQuery, User, UserListDto>
    {
        public GetUsersQueryHandler(IAppDbContext context, IMapper mapper) : base(context, mapper)
        {
        }

        protected override bool ApplySoftDeleteFilter(GetUsersQuery request) => !request.IncludeDeleted;

        protected override IQueryable<User> ApplySorting(IQueryable<User> query, Dictionary<string, string>? sort)
        {
            if (sort == null || sort.Count == 0) return query;
            var (column, direction) = sort.First();
            var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
            return column.ToLowerInvariant() switch
            {
                "fullname" => descending ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
                "email" => descending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "updatedat" => descending ? query.OrderByDescending(u => u.UpdatedAt) : query.OrderBy(u => u.UpdatedAt),
                _ => descending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt)
            };
        }

        protected override IQueryable<User> BuildQuery(IQueryable<User> query, GetUsersQuery request)
        {
            if (request.Page < 1 || request.PageSize is < 1 or > 100)
                throw new ArgumentOutOfRangeException(nameof(request.PageSize), "Trang hoặc kích thước trang không hợp lệ");
            if (request.Role.HasValue && !Enum.IsDefined(request.Role.Value))
                throw new ArgumentException("Vai trò không hợp lệ", nameof(request.Role));
            query = query.AsNoTracking();

            // 2. logic filter keyword 
            if(!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim().ToLower();
                query = query.Where(u => u.Email.ToLower().Contains(keyword) ||
                u.FullName.ToLower().Contains(keyword));
            }

            // 3. logic filter active
            if(request.Role.HasValue)
            {
                query = query.Where(u => u.Role == request.Role.Value);
            }

            if(request.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == request.IsActive.Value);
            }

            if (request.IncludeDeleted)
            {
                query = query.IgnoreQueryFilters();
            }

            // add sort
            if (!string.IsNullOrEmpty(request.SortColumn))
            {
                // Chuẩn hóa chiều sort (asc hoặc desc)
                bool isDesc = request.SortOrder?.ToLower() == "desc";
                string sortCol = request.SortColumn.ToLower();

                // Dùng switch expression để map tên cột từ FE sang Property của BE
                query = sortCol switch
                {
                    "fullname" => isDesc ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
                    "email" => isDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                    "createdat" => isDesc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
                    "updatedat" => isDesc ? query.OrderByDescending(u => u.UpdatedAt) : query.OrderBy(u => u.UpdatedAt),

                    // Mặc định nếu gửi tên cột linh tinh thì sort theo ngày tạo
                    _ => isDesc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt)
                };
            }
            else
            {
                // Mặc định: Nếu không nói gì thì user mới nhất lên đầu
                query = query.OrderByDescending(u => u.CreatedAt);
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

            return query;
        }
    }
}
