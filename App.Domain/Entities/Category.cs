
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities
{
    public class Category : BaseEntity
    {
        public string CodeType { get; set; } // ma_dinh_danh : hoi_dong_thi
        public string Code { get; set; }  // ma : DT02
        public string Name { get; set; } // ten : ha noi 2
        public string Description { get; set; } // mo ta
        public Guid? ParentId { get; set; } // danh muc cap tren id cua cha
        public int OrderIndex { get; set; } // Sắp xếp: Part 1 phải đứng trước Part 2
        public bool IsActive { get; set; } = true;
        [ForeignKey("ParentId")]
        public virtual Category? Parent { get; set; }
        public ICollection<Category> Children { get; set; } =  new List<Category>();

    }
}
