using App.Domain.Entities;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.DTOs
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string CodeType { get; set; } = string.Empty;  // exam-period, exam-board, exam-room...
        public string? Code { get; set; }                     // DOT2025-01
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ParentId { get; set; }
        public string? ParentName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public int ChildrenCount { get; set; }
        public List<CategoryDto>? Children { get; set; }
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


    public class CategoryDetailDto
    {
        public Guid Id { get; set; }
        public string CodeType { get; set; }
        public string? Code { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public Guid? ParentId { get; set; }
        public int Level { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public CategoryDto? Parent { get; set; }
        public List<CategoryDto>? Children { get; set; }
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
            // category -> categoryDto
            CreateMap<Category, CategoryDto>()
                 .ForMember(x => x.Children, opt => opt.MapFrom(x => x.Children))
             .ForMember(dest => dest.ChildrenCount,
                 opt => opt.MapFrom(src => src.Children.Count));


            // category -> categoryTreedto
            CreateMap<Category, CategoryTreeDto>()
            .ForMember(dest => dest.Children,
                opt => opt.MapFrom(src => src.Children));

            // ===============================
            // Category -> CategoryDetailDto
            // ===============================
            CreateMap<Category, CategoryDetailDto>()
                .ForMember(dest => dest.Parent,
                    opt => opt.MapFrom(src => src.Parent))
                .ForMember(dest => dest.Children,
                    opt => opt.MapFrom(src => src.Children));

            // ===============================
            // Create / Update
            // ===============================
            CreateMap<CreateCategoryDto, Category>()
                .ForMember(dest => dest.IsActive,
                    opt => opt.MapFrom(src => src.IsActive ?? true));

            CreateMap<UpdateCategoryDto, Category>()
                .ForAllMembers(opt => opt.Condition(
                    (src, dest, srcMember) => srcMember != null
                ));
        }
    }
}
