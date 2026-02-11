using App.Application.DTOs;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Categories.Queries
{
    public record GetCategoryDetailQuery(Guid CategoryId) : IRequest<CategoryDetailDto>;
     public class GetCategoryDetailQueryHandler : IRequestHandler<GetCategoryDetailQuery, CategoryDetailDto>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<GetCategoryDetailQueryHandler> _logger;

        public GetCategoryDetailQueryHandler(IAppDbContext dbContext, ILogger<GetCategoryDetailQueryHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<CategoryDetailDto> Handle(GetCategoryDetailQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var category = await _dbContext.Categories
                    .AsNoTracking()
                    .Include(c => c.Parent)
                    .Include(c => c.Children)
                    .Where(c => c.Id == request.CategoryId && !c.IsDeleted)
                    .Select(c => new CategoryDetailDto
                    {
                        Id = c.Id,
                        CodeType = c.CodeType,
                        Code = c.Code,
                        Name = c.Name,
                        Description = c.Description,
                        ParentId = c.ParentId,
                        IsActive = c.IsActive,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        Parent = c.Parent != null && !c.Parent.IsDeleted ? new CategoryDto
                        {
                            Id = c.Parent.Id,
                            CodeType = c.Parent.CodeType,
                            Code = c.Parent.Code,
                            Name = c.Parent.Name,
                            IsActive = c.Parent.IsActive
                        } : null,
                        Children = c.Children != null ?  c.Children
                        .Where(child => !child.IsDeleted)
                        .Select(child => new CategoryDto
                        {
                            Id = child.Id,
                            CodeType = child.CodeType,
                            Code = child.Code,
                            Name = child.Name,
                            ChildrenCount = c.Children.Count(x => !x.IsDeleted),
                            Description = child.Description,
                            IsActive = child.IsActive,
                            CreatedAt = child.CreatedAt,
                            UpdatedAt = child.UpdatedAt
                        }).ToList() : new List<CategoryDto>()
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (category == null)
                {
                    throw new KeyNotFoundException("Danh mục không tồn tại");
                }

                return category;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception("Có lỗi xảy ra khi lấy thông tin danh mục");
            }
        }
    }
}
