using App.Domain.Entities;
using AutoMapper;

namespace App.Application.Categories
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public int Module { get; set; } // phân hệ của danh mục
        public string CodeType { get; set; } // ma_dinh_danh : hoi_dong_thi
        public string Code { get; set; }  // ma : DT02
        public string Name { get; set; } // ten : ha noi 2
        public string Description { get; set; } // mo ta
        public int OrderIndex { get; set; } // Sắp xếp: Part 1 phải đứng trước Part 2
        public bool IsActive { get; set; } = true;
        public Guid? ReferenceId { get; set; }

        public List<CategoryDto>? Children { get; set; }
    }

    public class CategoryFilter
    {
        public int? Module { get; set; }

        public string? CodeType { get; set; }

        public string? Code { get; set; }

        public string? Name { get; set; }

        public bool? IsActive { get; set; }

        public Guid? ReferenceId { get; set; }
    }

    public class CategoryTreeDto
    {
        public Guid Id { get; set; }
        public string CodeType { get; set; }
        public string? Code { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public Guid? ParentId { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int Level { get; set; }
        public List<CategoryTreeDto>? Children { get; set; }
    }


    public class CodeTypeDto
    {
        public string CodeType { get; set; }
        public int Count { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
    }
    public class CreateCategoryDto
    {
        public string CodeType { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public Guid? ParentId { get; set; }
        public bool? IsActive { get; set; }
    }
    public class UpdateCategoryDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public int? Level { get; set; }
        public string? Description { get; set; }
        public Guid? ParentId { get; set; }
        public bool? IsActive { get; set; }
        public bool? DeactivateChildren { get; set; }
    }

    public class CategorySelectDto
    {
        public Guid value { get; set; }
        public string label { get; set; }
    }

    public class CategoryProfile : Profile
    {
        public CategoryProfile()
        {
            CreateMap<Category, CategoryDto>();
            CreateMap<Category, CategoryTreeDto>();

            CreateMap<CreateCategoryDto, Category>()
                .ForMember(
                    dest => dest.IsActive,
                    opt => opt.MapFrom(src => src.IsActive ?? true));

            CreateMap<UpdateCategoryDto, Category>()
                .ForAllMembers(opt => opt.Condition(
                    (src, dest, srcMember) => srcMember != null));
        }
    }
}
