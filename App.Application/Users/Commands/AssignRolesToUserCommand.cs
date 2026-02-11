using App.Application.Users.Commands;
using App.Domain.Entities;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Users.Commands
{
    public record AssignRolesToUserCommand(Guid UserId, List<Guid> RoleIds, Guid AssignedBy) : IRequest<bool>;

    public class AssignRolesToUserCommandHandler : IRequestHandler<AssignRolesToUserCommand, bool>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<AssignRolesToUserCommandHandler> _logger;

        public AssignRolesToUserCommandHandler(IAppDbContext dbContext, ILogger<AssignRolesToUserCommandHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> Handle(AssignRolesToUserCommand request, CancellationToken cancellationToken)
        {
            // 1. Clean input: Loại bỏ RoleId trùng lặp nếu frontend gửi lên duplicate
            var requestedRoleIds = request.RoleIds.Distinct().ToList();

            // 2. Validate User tồn tại
            var userExists = await _dbContext.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists) throw new KeyNotFoundException($"User {request.UserId} không tồn tại.");

            // 3. Validate Role tồn tại (Chỉ đếm số lượng role hợp lệ)
            var validRoleCount = await _dbContext.Roles
                .CountAsync(r => requestedRoleIds.Contains(r.Id), cancellationToken);

            if (validRoleCount != requestedRoleIds.Count)
            {
                throw new KeyNotFoundException("Một hoặc nhiều Role ID không tồn tại trong hệ thống.");
            }

            // --- BẮT ĐẦU TRANSACTION ---
            using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

            try
            {
                // 4. Lấy danh sách Role hiện tại của User (Lấy cả entity để xóa cho dễ)
                var currentRoles = await _dbContext.UserRoles
                    .Where(ur => ur.UserId == request.UserId)
                    .ToListAsync(cancellationToken);

                var currentRoleIds = currentRoles.Select(x => x.RoleId).ToList();

                // 5. Tính toán Delta (Cái nào cần xóa, cái nào cần thêm)

                // Cần thêm: Có trong Request nhưng chưa có trong DB
                var rolesToAddIds = requestedRoleIds.Except(currentRoleIds).ToList();

                // Cần xóa: Có trong DB nhưng không có trong Request
                var rolesToDelete = currentRoles.Where(ur => !requestedRoleIds.Contains(ur.RoleId)).ToList();

                // 6. Thực thi Xóa
                if (rolesToDelete.Any())
                {
                    _dbContext.UserRoles.RemoveRange(rolesToDelete);
                }

                // 7. Thực thi Thêm
                if (rolesToAddIds.Any())
                {
                    var newUserRoles = rolesToAddIds.Select(roleId => new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = request.UserId,
                        RoleId = roleId,
                        AssignedAt = DateTime.UtcNow,
                        AssignedBy = request.AssignedBy
                    });

                    _dbContext.UserRoles.AddRange(newUserRoles);
                }

                // 8. Lưu và Commit (Chỉ chạy SQL nếu có thay đổi)
                if (rolesToDelete.Any() || rolesToAddIds.Any())
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Updated roles for user {UserId}. Added: {Added}, Removed: {Removed}",
                        request.UserId, rolesToAddIds.Count, rolesToDelete.Count);
                }
                else
                {
                    // Không có gì thay đổi cũng commit để đóng transaction sạch sẽ
                    await transaction.CommitAsync(cancellationToken);
                    _logger.LogInformation("No role changes for user {UserId}", request.UserId);
                }

                return true;
            }
            catch (Exception ex)
            {
                // Chỉ cần log, transaction sẽ tự rollback khi ra khỏi khối using
                _logger.LogError(ex, "Error assigning roles to user {UserId}", request.UserId);
                throw; // Ném lỗi ra để tầng trên xử lý (trả về 500)
            }
        }
    }
}