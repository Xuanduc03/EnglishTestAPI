using App.Domain.Shares;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Entities
{
    public abstract class BaseEntity : ISoftDelete
    {
        // ✨ ID mặc định kiểu GUID
        public Guid Id { get; set; } = Guid.NewGuid();

        // ===================
        //  TRACKING
        // ===================
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }

        public DateTime UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }

        // ===================
        //  SOFT DELETE (Triển khai Interface)
        // ===================

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        public Guid? DeletedBy { get; set; }

    }
}
