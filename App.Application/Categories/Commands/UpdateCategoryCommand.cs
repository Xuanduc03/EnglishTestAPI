using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Categories.Commands
{
    // CẬP NHẬT DANH MỤC
    public record UpdateCategoryCommand(Guid Id, UpdateCategoryDto Data) : IRequest<bool>;

    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, bool>
    {
        private readonly IAppDbContext _dbContext;

        public UpdateCategoryCommandHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _dbContext.Categories.FindAsync(request.Id);

            if (category == null)
                throw new KeyNotFoundException("Danh mục không tồn tại");

            if (category.IsDeleted)
                throw new InvalidOperationException("Danh mục đã bị xóa");

            var dto = request.Data;

            // Update Code (check duplicate)
            if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code != category.Code)
            {
                var exists = await _dbContext.Categories
                    .AnyAsync(c => c.CodeType == category.CodeType && c.Code == dto.Code && c.Id != request.Id,
                        cancellationToken);

                if (exists)
                    throw new InvalidOperationException($"Mã '{dto.Code}' đã tồn tại");

                category.Code = dto.Code.Trim();
            }

            // Update các field
            if (!string.IsNullOrWhiteSpace(dto.Name))
                category.Name = dto.Name.Trim();

            if (dto.Description != null)
                category.Description = dto.Description.Trim();
           
            // Update ParentId
            if (dto.ParentId != category.ParentId)
            {
                if (dto.ParentId.HasValue)
                {
                    // Không cho phép set chính mình là parent
                    if (dto.ParentId.Value == request.Id)
                        throw new InvalidOperationException("Không thể đặt chính mình làm parent");

                    var parent = await _dbContext.Categories.FindAsync(dto.ParentId.Value);
                    if (parent == null)
                        throw new InvalidOperationException("Danh mục cha không tồn tại");

                    if (parent.CodeType != category.CodeType)
                        throw new InvalidOperationException("Danh mục cha phải cùng CodeType");

                    // Check circular reference
                    if (await IsCircular(dto.ParentId.Value, request.Id, cancellationToken))
                        throw new InvalidOperationException("Không thể tạo vòng lặp trong cây danh mục");
                }

                category.ParentId = dto.ParentId;
            }

            if (dto.IsActive.HasValue)
                category.IsActive = dto.IsActive.Value;

            category.UpdatedAt = DateTime.Now;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        private async Task<bool> IsCircular(Guid parentId, Guid currentId, CancellationToken cancellationToken)
        {
            var current = parentId;
            var visited = new HashSet<Guid>();

            while (current != Guid.Empty)
            {
                if (current == currentId)
                    return true;

                if (visited.Contains(current))
                    return true;

                visited.Add(current);

                var parent = await _dbContext.Categories
                    .Where(c => c.Id == current)
                    .Select(c => c.ParentId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!parent.HasValue)
                    break;

                current = parent.Value;
            }

            return false;
        }
    }
}