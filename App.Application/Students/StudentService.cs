using App.Application.Interfaces;
using App.Domain.Entities;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace App.Application.Students
{
    public class StudentService : IStudentService
    {
        private readonly IRepository<Student> _repository;
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<StudentService> _logger;

        //ctor
        public StudentService(
            IRepository<Student> repository,
            IRepository<User> userRepository,
            IUnitOfWork uow,
            IMapper mapper,
            ILogger<StudentService> logger)
        {
            _repository = repository;
            _userRepository = userRepository;
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        // get by  id 
        public async Task<StudentDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _repository.SelectOne(s => s.Id == id, ct);
            return entity is null ? null : _mapper.Map<StudentDto>(entity);
        }



        // ==================== GET BY USER ID ====================
        public async Task<StudentDto?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            var entity = await _repository.SelectOne(s => s.UserId == userId, ct);
            return entity is null ? null : _mapper.Map<StudentDto>(entity);
        }

        // ==================== GET ALL (Filter + Paging) ====================
        public async Task<(IList<StudentDto> Items, int Total)> GetAllAsync(
            StudentFilter filter, CancellationToken ct = default)
        {
            // Build điều kiện lọc
            var where = BuildFilter(filter);

            // Query + phân trang
            var (items, total) = await _repository.SelectPaged(
                where: where,
                page: filter.Page - 1,   // Repository dùng 0-based
                pageSize: filter.PageSize,
                ct: ct);

            var dtos = _mapper.Map<IList<StudentDto>>(items);
            return (dtos, total);
        }

        // ==================== CREATE ====================
        public async Task<StudentDto> CreateAsync(CreateStudentDto dto, CancellationToken ct = default)
        {
            // 1. Check User tồn tại
            var user = await _userRepository.SelectById(dto.UserId, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy User với Id = {dto.UserId}");

            // 2. Check User đã có Student profile chưa
            var existed = await _repository.Exists(s => s.UserId == dto.UserId, ct);
            if (existed)
                throw new InvalidOperationException($"User '{user.Email}' đã có Student profile.");

            // 3. Map + Insert
            var entity = _mapper.Map<Student>(dto);
            entity.Points = 0;
            entity.Streak = 0;
            entity.MemberLevel = MemberLevel.Standard;

            await _repository.Insert(entity, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Đã tạo Student cho User: {UserId}", dto.UserId);

            // Load lại User để map FullName/Email
            entity.User = user;
            return _mapper.Map<StudentDto>(entity);
        }

        // ==================== UPDATE ====================
        public async Task UpdateAsync(Guid id, UpdateStudentDto dto, CancellationToken ct = default)
        {
            var entity = await _repository.SelectById(id, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy Student với Id = {id}");

            // Map (chỉ field khác null)
            _mapper.Map(dto, entity);

            await _repository.Update(entity, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Đã cập nhật Student: {Id}", id);
        }

        // ==================== DELETE (SOFT) ====================
        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _repository.SelectById(id, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy Student với Id = {id}");

            entity.IsDeleted = true;
            await _repository.Update(entity, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Đã xóa mềm Student: {Id}", id);
        }

        // ==================== RESTORE ====================
        public async Task RestoreAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _repository.SelectById(id, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy Student với Id = {id}");

            entity.IsDeleted = false;
            await _repository.Update(entity, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Đã khôi phục Student: {Id}", id);
        }

        // ==================== PRIVATE HELPERS ====================
        private static Expression<Func<Student, bool>>? BuildFilter(StudentFilter filter)
        {
            var hasKeyword = !string.IsNullOrWhiteSpace(filter.Keyword);
            var hasLevel = filter.MemberLevel.HasValue;
            var hasMinPoints = filter.MinPoints.HasValue;
            var hasMinStreak = filter.MinStreak.HasValue;

            if (!hasKeyword && !hasLevel && !hasMinPoints && !hasMinStreak)
                return null;

            return s =>
                (!hasKeyword ||
                    s.User.FullName.Contains(filter.Keyword!) ||
                    s.User.Email.Contains(filter.Keyword!)) &&
                (!hasLevel || s.MemberLevel == filter.MemberLevel) &&
                (!hasMinPoints || s.Points >= filter.MinPoints) &&
                (!hasMinStreak || s.Streak >= filter.MinStreak);
        }
    }
}
