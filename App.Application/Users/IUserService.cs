using App.Application.DTOs;
using App.Domain.Identity;

namespace App.Application.Users
{
    public interface IUserService
    {
        Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<UserDto?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<IList<UserDto>> GetByIdsAsync(Guid[] ids, CancellationToken ct = default);
        Task<(IList<UserDto> Items, int Total)> GetAllAsync(
            GetUsersFilter filter, CancellationToken ct = default);

        // Ghi
        Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default);
        Task UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
        Task RestoreAsync(Guid id, CancellationToken ct = default);

        // Role
        Task ChangeRoleAsync(Guid id, UserRole newRole, CancellationToken ct = default);
        Task ToggleActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
    }
}
