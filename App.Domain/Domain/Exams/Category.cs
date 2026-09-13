
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities
{
    public class Category : BaseEntity
    {
        public int Module { get; set; } // phân hệ của danh mục
        public string CodeType { get; set; } // ma_dinh_danh : hoi_dong_thi
        public string Code { get; set; }  // ma : DT02
        public string Name { get; set; } // ten : ha noi 2
        public string Description { get; set; } // mo ta
        public int OrderIndex { get; set; } // Sắp xếp: Part 1 phải đứng trước Part 2
        public bool IsActive { get; set; } = true;        
        public  Guid? ReferenceId { get; set; }     

    }
}
