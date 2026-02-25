using App.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using App.Application.Share;
using App.Domain.Entities;
using AutoMapper;
using App.Application.Interfaces;

namespace App.Application.Users.Queries
{

    public record GetUsersQuery : BaseGetAllQuery<UserListDto>
    {
        // filter of user
        public List<Guid>? RoleIds { get; init; }
        public bool? IsActive { get; init; }
        public bool? IsEmailVerified { get; init; }
        public string? SortColumn { get; init; }
        public string? SortOrder {  get; init; }
        public bool IncludeDeleted { get; init; } = false;
    }

    public class GetUsersQueryHandler : BaseQueryHandler<GetUsersQuery, User, UserListDto>
    {
        public GetUsersQueryHandler(IAppDbContext context, IMapper mapper) : base(context, mapper)
        {
        }

        protected override IQueryable<User> BuildQuery(IQueryable<User> query, GetUsersQuery request)
        {
            //  1. include roles    
            query = query.Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role);

            // 2. logic filter keyword 
            if(!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim().ToLower();
                query = query.Where(u => u.Email.ToLower().Contains(keyword) ||
                (u.Fullname != null && u.Fullname.ToLower().Contains(keyword)));
            }

            // 3. logic filter active
            if(request.RoleIds != null && request.RoleIds.Any())
            {
                query = query.Where(u => u.UserRoles.Any(ur => request.RoleIds.Contains(ur.RoleId)));
            }

            if(request.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == request.IsActive.Value);
            }

            if (request.IsEmailVerified.HasValue)
            {
                query = query.Where(u => u.EmailVerified == request.IsEmailVerified.Value);
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
                    "fullname" => isDesc ? query.OrderByDescending(u => u.Fullname) : query.OrderBy(u => u.Fullname),
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
