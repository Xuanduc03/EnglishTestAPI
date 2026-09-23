namespace App.Application.Categories
{
    public interface ICategoryService
    {
        // ===== CRUD cơ bản =====
        Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default);
        Task UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
        Task RestoreAsync(Guid id, CancellationToken ct = default);
        Task ToggleActiveAsync(Guid id, bool isActive, CancellationToken ct = default);

        // ===== TREE =====
        /// <summary>Lấy toàn bộ cây theo CodeType</summary>
        Task<IList<CategoryTreeDto>> GetTreeAsync(string codeType, CancellationToken ct = default);

        /// <summary>Lấy cây con (từ 1 node trở xuống)</summary>
        Task<CategoryTreeDto?> GetSubTreeAsync(Guid rootId, CancellationToken ct = default);

        /// <summary>Lấy danh sách phẳng theo CodeType (có filter)</summary>
        Task<IList<CategoryDto>> GetAllAsync(CategoryFilter filter, CancellationToken ct = default);

        /// <summary>Thống kê theo CodeType</summary>
        Task<IList<CodeTypeDto>> GetCodeTypeStatsAsync(CancellationToken ct = default);

        /// <summary>Cho dropdown: lấy theo CodeType</summary>
        Task<IList<CategorySelectDto>> GetSelectListAsync(string codeType, CancellationToken ct = default);

        /// <summary>Lấy danh sách tổ tiên (ancestors) của 1 node</summary>
        Task<IList<CategoryDto>> GetAncestorsAsync(Guid id, CancellationToken ct = default);
    }
}
