using App.Domain.Entities;

namespace App.Domain.Domain.Logging
{
    public class ActivityLog : BaseEntity
    {
        public Guid? UserId { get; set; }

        public string Action { get; set; } = null!;

        public string Module { get; set; } = null!;

        public string? EntityName { get; set; }

        public Guid? EntityId { get; set; }

        public string? Description { get; set; }

        public string? MetadataJson { get; set; }

        public string? IpAddress { get; set; }

        public virtual User? User { get; set; }
    }
}
