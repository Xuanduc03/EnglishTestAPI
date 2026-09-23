

namespace App.Domain.Entities
{
    public class Category : BaseEntity
    {
        public int Module { get; set; } // phân hệ của danh mục
        public string CodeType { get; set; } // ma_dinh_danh : x1
        public string Code { get; set; }  // ma : DT02
        public string Name { get; set; } // ten : ha noi 2
        public string Description { get; set; } // mo ta
        public int OrderIndex { get; set; } // Sắp xếp: Part 1 phải đứng trước Part 2
        public bool IsActive { get; set; } = true;
        public Guid? ParentId { get; set; }
        public Category? Parent { get; set; }
        public ICollection<Category> Children { get; set; } = new List<Category>();

    }
}
