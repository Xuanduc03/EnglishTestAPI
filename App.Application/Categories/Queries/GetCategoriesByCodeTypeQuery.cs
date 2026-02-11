using App.Application.DTOs;
using App.Application.Interfaces;
using CloudinaryDotNet.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection.Emit;


namespace App.Application.Categories.Queries
{
    // GET CATEGORIES BY CODE TYPE
    public record GetCategoriesByCodeTypeQuery(string CodeType, Guid parentId) : IRequest<List<CategoryDto>>;

    public class GetCategoriesByCodeTypeQueryHandler : IRequestHandler<GetCategoriesByCodeTypeQuery, List<CategoryDto>>
    {
        private readonly IAppDbContext _dbContext;

        public GetCategoriesByCodeTypeQueryHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<CategoryDto>> Handle(GetCategoriesByCodeTypeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var codeType = request.CodeType?.Trim().ToLower();

                var query = _dbContext.Categories.AsNoTracking().Where(c => !c.IsDeleted);

                // filter of codetype
                if(request.CodeType != null && !string.IsNullOrEmpty(request.CodeType))
                {
                    query = query.Where(c => c.CodeType.ToLower() == codeType);
                }

                
                query = query.Where(c => c.ParentId == request.parentId);
                var categories = await query
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Code = c.Code,
                        Name = c.Name,
                        Description = c.Description,
                        CreatedAt = c.CreatedAt,
                        IsActive = c.IsActive,
                        ParentId = c.ParentId,
                        ParentName = c.Parent != null ? c.Parent.Name : null,
                        ChildrenCount = c.Children.Count(child => !child.IsDeleted),

                        // map data children
                        Children  = c.Children
                            .Where(child => !child.IsDeleted)
                            .Select(child => new CategoryDto 
                            { 
                                Id = child.Id,
                                Code = child.Code,
                                Name = child.Name,
                                Description = child.Description,
                                IsActive = child.IsActive,
                                ParentId = child.ParentId,
                                ChildrenCount = child.Children.Count(grandChild => !grandChild.IsDeleted)
                            })
                            .OrderBy(child => child.Name)
                            .ToList()
                    })
                    .OrderBy(c => c.Name)
                  .ToListAsync(cancellationToken); ;
                return categories;

            }
            catch (Exception ex)
            {
                throw new Exception("Có lỗi xảy ra khi lấy danh sách danh mục theo loại");
            }
        }
    }
}
