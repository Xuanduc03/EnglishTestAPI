using App.Application.DTOs;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Categories.Queries
{
    public record GetCategorySelectQuery(string? CodeType) : IRequest<List<CategorySelectDto>>;

    public class GetCategorySelectQueryHandler : IRequestHandler<GetCategorySelectQuery, List<CategorySelectDto>> {
        private readonly IAppDbContext _context;

        public GetCategorySelectQueryHandler(IAppDbContext context) 
        {
            _context = context;
        }

        public async Task<List<CategorySelectDto>> Handle(GetCategorySelectQuery request, CancellationToken cancellation)
        {
            var query = _context.Categories.AsNoTracking()
                .Where(c =>
                    !c.IsDeleted &&
                    c.IsActive
                );
            if (!string.IsNullOrEmpty(request.CodeType)) 
            {
                query = query.Where(c => c.Name.Contains(request.CodeType));
            }

            return await query
                .OrderBy(c => c.CodeType) 
                .ThenBy(c => c.Name)
                .Select(c => new CategorySelectDto
                {
                    label = $"{c.Name} ({c.CodeType})", 
                    value = c.Id
                }).ToListAsync(cancellation);

        }
    }
}
