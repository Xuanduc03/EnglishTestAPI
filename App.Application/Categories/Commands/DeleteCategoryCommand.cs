using App.Application.Interfaces;
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
    // DELETE CATEGORY
    public record DeleteCategoryCommand(Guid CategoryId, bool DeleteChildren = false) : IRequest<bool>;

    public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, bool>
    {
        private readonly IAppDbContext _dbContext;

        public DeleteCategoryCommandHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

            try
            {
                var category = await _dbContext.Categories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(
                    c => c.Id == request.CategoryId && !c.IsDeleted,
                    cancellationToken);

                if (category == null)
                {
                    throw new KeyNotFoundException("Danh mục không tồn tại hoặc đã bị xóa");
                }

                // Có con nhưng không cho xóa
                if (category.Children.Any(c => !c.IsDeleted) && !request.DeleteChildren)
                {
                    throw new InvalidOperationException(
                        "Danh mục có danh mục con. Vui lòng xóa con trước hoặc bật tùy chọn xóa cả cây.");
                }

                // Xóa đệ quy (soft)
                await SoftDeleteRecursive(category.Id, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return true;
            }
            catch (KeyNotFoundException)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            catch (InvalidOperationException)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task SoftDeleteRecursive(Guid parentId, CancellationToken cancellationToken)
        {
            var categories = await _dbContext.Categories
                .Where(c => c.ParentId == parentId)
                .ToListAsync(cancellationToken);

           foreach(var child in categories)
            {
                SoftDeleteRecursive(child.Id, cancellationToken);

                child.IsDeleted = true; 
                child.UpdatedAt = DateTime.UtcNow;
            }

           var parent = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id ==  parentId, cancellationToken);

            if(parent != null)
            {
                parent.IsDeleted = true;
                parent.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

}
