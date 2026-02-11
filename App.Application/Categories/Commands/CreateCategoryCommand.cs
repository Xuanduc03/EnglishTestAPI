using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using FluentValidation;
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
    // TẠO DANH MỤC
    public record CreateCategoryCommand(CreateCategoryDto Data) : IRequest<Guid>;

    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Guid>
    {
        private readonly IAppDbContext _dbContext;

        public CreateCategoryCommandHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Data;

            // xử lý quan hệ cha con
            if(dto.ParentId.HasValue)
            {
                var parent = await _dbContext.Categories.FindAsync(new object[] { dto.ParentId.Value }, cancellationToken);

                if (parent == null)
                    throw new InvalidOperationException("Danh mục cha không tồn tại");

                dto.CodeType = parent.CodeType;
            }
            else
            {
                if (string.IsNullOrEmpty(dto.CodeType))
                    throw new InvalidOperationException("Danh mục gốc phải có Mã định danh (CodeType)");
            }

            if (string.IsNullOrEmpty(request.Data.CodeType))
            {
                throw new Exception("Mã định danh không được để trống");
            }

            var existedCategory = await CheckDuplicateAsync(dto, cancellationToken);

            if (existedCategory != null && existedCategory.IsDeleted)
            {
                // 👉 KHÔI PHỤC CATEGORY
                existedCategory.IsDeleted = false;
                existedCategory.Name = dto.Name.Trim();
                existedCategory.Description = dto.Description?.Trim() ?? string.Empty;
                existedCategory.ParentId = dto.ParentId;
                existedCategory.IsActive = dto.IsActive ?? true;
                existedCategory.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);
                return existedCategory.Id;
            }


            var category = new Category
            {
                Id = Guid.NewGuid(),
                CodeType = dto.CodeType.Trim(),
                Code = dto.Code?.Trim() ?? string.Empty,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                ParentId = dto.ParentId,
                IsActive = dto.IsActive ?? true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return category.Id;
        }
        private async Task<Category?> CheckDuplicateAsync(CreateCategoryDto dto, CancellationToken cancellationToken)
        {
            var codeType = dto.CodeType.Trim();
            var code = dto.Code?.Trim();
            if (string.IsNullOrWhiteSpace(dto.Code)) return null;

            var exists = await _dbContext.Categories
                .FirstOrDefaultAsync(c => c.CodeType == codeType && c.Code == code, cancellationToken);

            if (exists != null && !exists.IsDeleted)
                throw new InvalidOperationException($"Mã '{dto.Code}' đã tồn tại");

            return exists;
        }


    }
}