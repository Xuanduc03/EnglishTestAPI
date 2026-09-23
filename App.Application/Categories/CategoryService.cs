

using App.Application.Interfaces;
using App.Domain.Entities;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace App.Application.Categories
{
    public class CategoryService : ICategoryService
    {
        private readonly IRepository<Category> _repository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ICacheService _cache;

        private readonly ILogger<CategoryService> _logger;


        // ===== CACHE KEYS =====
        private const string KeyPrefix = "category";
        private static string KeyById(Guid id) => $"{KeyPrefix}:id:{id}";
        private static string KeyTree(string codeType) => $"{KeyPrefix}:tree:{codeType}";
        private static string KeySelect(string codeType) => $"{KeyPrefix}:select:{codeType}";
        private const string KeyStats = $"{KeyPrefix}:stats";


        public CategoryService(
            IRepository<Category> repository,
            IUnitOfWork uow,
            IMapper mapper,
            ILogger<CategoryService> logger)
        {
            _repository = repository;
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        // ==================== GET BY ID ====================
        public async Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _repository.SelectById(id, ct);
            return entity is null ? null : _mapper.Map<CategoryDto>(entity);
        }

        // ==================== GET ALL (phẳng, có filter) ====================
        public async Task<IList<CategoryDto>> GetAllAsync(CategoryFilter filter, CancellationToken ct = default)
        {
            var where = BuildFilter(filter);

            var items = await _repository.Select(where, ct);

            return _mapper.Map<IList<CategoryDto>>(items);
        }

        // ==================== GET TREE (toàn bộ cây theo CodeType) ====================
        public async Task<IList<CategoryTreeDto>> GetTreeAsync(string codeType, CancellationToken ct = default)
        {
            // 1. Lấy TẤT CẢ category theo CodeType (1 query duy nhất)
            var all = await _repository.Select(
                c => c.CodeType == codeType && !c.IsDeleted, ct);

            // 2. Map sang TreeDto
            var flat = _mapper.Map<List<CategoryTreeDto>>(all);

            // 3. Build cây từ danh sách phẳng
            return BuildTree(flat);
        }

        // ==================== GET SUB TREE (từ 1 node) ====================
        public async Task<CategoryTreeDto?> GetSubTreeAsync(Guid rootId, CancellationToken ct = default)
        {
            var all = await _repository.Select(c => !c.IsDeleted, ct);

            var flat = _mapper.Map<List<CategoryTreeDto>>(all);
            var tree = BuildTree(flat);

            // Tìm node gốc trong cây
            return FindNode(tree, rootId);
        }

        // ==================== GET CODE TYPE STATS ====================
        public async Task<IList<CodeTypeDto>> GetCodeTypeStatsAsync(CancellationToken ct = default)
        {
            var all = await _repository.Select(c => !c.IsDeleted, ct);

            return all
                .GroupBy(c => c.CodeType)
                .Select(g => new CodeTypeDto
                {
                    CodeType = g.Key,
                    Count = g.Count(),
                    ActiveCount = g.Count(x => x.IsActive),
                    InactiveCount = g.Count(x => !x.IsActive)
                })
                .ToList();
        }

        // ==================== GET SELECT LIST (dropdown) ====================
        public async Task<IList<CategorySelectDto>> GetSelectListAsync(string codeType, CancellationToken ct = default)
        {
            var items = await _repository.Select(
                c => c.CodeType == codeType
                     && c.IsActive
                     && !c.IsDeleted, ct);

            return items.Select(c => new CategorySelectDto
            {
                value = c.Id,
                label = c.Name
            }).ToList();
        }

        // ==================== GET ANCESTORS ====================
        public async Task<IList<CategoryDto>> GetAncestorsAsync(Guid id, CancellationToken ct = default)
        {
            var all = await _repository.Select(c => !c.IsDeleted, ct);
            var dict = all.ToDictionary(c => c.Id);

            var ancestors = new List<Category>();
            var current = dict.GetValueOrDefault(id);

            while (current?.ParentId is not null && dict.TryGetValue(current.ParentId.Value, out var parent))
            {
                ancestors.Add(parent);
                current = parent;
            }

            ancestors.Reverse(); // Từ gốc -> node hiện tại
            return _mapper.Map<IList<CategoryDto>>(ancestors);
        }

        // ==================== CREATE ====================
        public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default)
        {
            // 1. Validate CodeType + Code không trùng
            var exists = await _repository.Exists(
                c => c.CodeType == dto.CodeType && c.Code == dto.Code, ct);
            if (exists)
                throw new InvalidOperationException($"Code '{dto.Code}' đã tồn tại trong CodeType '{dto.CodeType}'.");

            // 2. Validate ParentId (nếu có)
            if (dto.ParentId.HasValue)
            {
                var parent = await _repository.SelectById(dto.ParentId.Value, ct)
                    ?? throw new KeyNotFoundException($"Parent không tồn tại: {dto.ParentId}");

                // Parent phải cùng CodeType
                if (parent.CodeType != dto.CodeType)
                    throw new InvalidOperationException("Parent phải cùng CodeType.");
            }

            // 3. Map + Insert
            var entity = _mapper.Map<Category>(dto);
            await _repository.Insert(entity, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Đã tạo Category: {Code} - {Name}", entity.Code, entity.Name);

            return _mapper.Map<CategoryDto>(entity);
        }

        // ==================== UPDATE ====================
        public async Task UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken ct = default)
        {
            var entity = await _repository.SelectById(id, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy Category: {id}");

            // 1. Check trùng Code (nếu có đổi)
            if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code != entity.Code)
            {
                var dup = await _repository.Exists(
                    c => c.CodeType == entity.CodeType
                         && c.Code == dto.Code
                         && c.Id != id, ct);
                if (dup)
                    throw new InvalidOperationException($"Code '{dto.Code}' đã tồn tại.");
            }

            // 2. Không cho phép chọn chính nó hoặc con của nó làm Parent
            if (dto.ParentId.HasValue)
            {
                if (dto.ParentId == id)
                    throw new InvalidOperationException("Không thể chọn chính nó làm Parent.");

                var isDescendant = await IsDescendantAsync(id, dto.ParentId.Value, ct);
                if (isDescendant)
                    throw new InvalidOperationException("Không thể chọn con của nó làm Parent.");
            }

            // 3. Map (chỉ field khác null)
            _mapper.Map(dto, entity);

            // 4. DeactivateChildren: tắt toàn bộ con cháu
            if (dto.DeactivateChildren == true && dto.IsActive == false)
            {
                await DeactivateDescendantsAsync(id, ct);
            }

            await _repository.Update(entity, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Đã cập nhật Category: {Id}", id);
        }

        // ==================== DELETE (SOFT) ====================
        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _repository.SelectById(id, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy Category: {id}");

            // Check còn con không?
            var hasChildren = await _repository.Exists(c => c.ParentId == id && !c.IsDeleted, ct);
            if (hasChildren)
                throw new InvalidOperationException("Không thể xóa vì còn danh mục con.");

            entity.IsDeleted = true;
            await _repository.Update(entity, ct);
            await _uow.SaveChangesAsync(ct);
        }

        // ==================== RESTORE ====================
        public async Task RestoreAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _repository.SelectById(id, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy Category: {id}");

            entity.IsDeleted = false;
            await _repository.Update(entity, ct);
            await _uow.SaveChangesAsync(ct);
        }

        // ==================== TOGGLE ACTIVE ====================
        public async Task ToggleActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
        {
            var entity = await _repository.SelectById(id, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy Category: {id}");

            if (entity.IsActive == isActive) return;

            entity.IsActive = isActive;

            // Nếu tắt -> tắt luôn con cháu
            if (!isActive)
                await DeactivateDescendantsAsync(id, ct);

            await _repository.Update(entity, ct);
            await _uow.SaveChangesAsync(ct);
        }

        // ==================== PRIVATE HELPERS ====================

        private static Expression<Func<Category, bool>>? BuildFilter(CategoryFilter filter)
        {
            if (string.IsNullOrWhiteSpace(filter.Name)
                && string.IsNullOrWhiteSpace(filter.CodeType)
                && filter.IsActive is null)
                return null;

            return c =>
                (string.IsNullOrWhiteSpace(filter.Name) || c.Name.Contains(filter.Name)) &&
                (string.IsNullOrWhiteSpace(filter.CodeType) || c.CodeType == filter.CodeType) &&
                (filter.IsActive == null || c.IsActive == filter.IsActive);
        }

        /// <summary>Build cây từ danh sách phẳng</summary>
        private static List<CategoryTreeDto> BuildTree(List<CategoryTreeDto> flat)
        {
            var dict = flat.ToDictionary(x => x.Id);
            var roots = new List<CategoryTreeDto>();

            foreach (var node in flat)
            {
                if (node.ParentId is null || !dict.ContainsKey(node.ParentId.Value))
                {
                    roots.Add(node);
                }
                else
                {
                    // Gắn vào parent
                    var parent = dict[node.ParentId.Value];
                    parent.Children ??= new List<CategoryTreeDto>();
                    parent.Children.Add(node);
                }
            }

            SortTree(roots);
            return roots;
        }

        private static void SortTree(List<CategoryTreeDto> nodes)
        {
           
            foreach (var node in nodes)
            {
                if (node.Children?.Count > 0)
                    SortTree(node.Children);
            }
        }

        /// <summary>Tìm node trong cây theo Id</summary>
        private static CategoryTreeDto? FindNode(List<CategoryTreeDto> nodes, Guid id)
        {
            foreach (var node in nodes)
            {
                if (node.Id == id) return node;
                if (node.Children?.Count > 0)
                {
                    var found = FindNode(node.Children, id);
                    if (found != null) return found;
                }
            }
            return null;
        }

        /// <summary>Kiểm tra targetId có phải con cháu của nodeId không</summary>
        private async Task<bool> IsDescendantAsync(Guid nodeId, Guid targetId, CancellationToken ct)
        {
            var all = await _repository.Select(c => !c.IsDeleted, ct);
            var dict = all.ToDictionary(c => c.Id);

            var current = dict.GetValueOrDefault(targetId);
            while (current?.ParentId is not null)
            {
                if (current.ParentId == nodeId) return true;
                current = dict.GetValueOrDefault(current.ParentId.Value);
            }
            return false;
        }

        /// <summary>Tắt toàn bộ con cháu (dùng cho DeactivateChildren)</summary>
        private async Task DeactivateDescendantsAsync(Guid rootId, CancellationToken ct)
        {
            var all = await _repository.Select(c => !c.IsDeleted, ct);
            var toDeactivate = new List<Category>();
            var queue = new Queue<Guid>();
            queue.Enqueue(rootId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                var children = all.Where(c => c.ParentId == currentId).ToList();

                foreach (var child in children)
                {
                    child.IsActive = false;
                    toDeactivate.Add(child);
                    queue.Enqueue(child.Id);
                }
            }

            if (toDeactivate.Count > 0)
                await _repository.UpdateMany(toDeactivate, ct);
        }
    }
}
