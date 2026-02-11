

namespace App.Domain.Entities
{
    // định nghĩa các quyền cụ thể trong hệ thống
    public class Permission : BaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Module { get; set; } = "user";
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    }
}
