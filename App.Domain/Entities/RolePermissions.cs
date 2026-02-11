using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Entities
{
    // gán quyền vai trò
    public class RolePermission : BaseEntity
    {
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }
        public DateTime AssignedAt { get; set; }  = DateTime.Now;
        public Guid AssignedBy { get; set; }

        public Role Role { get; set; }
        public Permission Permission { get; set; }
    }
}
