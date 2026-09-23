namespace App.Application.Students
{
    public interface IStudentService
    {
        // Crud
        /// <summary>Lấy Student theo Id</summary>
        Task<StudentDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>Lấy Student theo UserId (quan trọng - dùng khi user login)</summary>
        Task<StudentDto?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

        /// <summary>Lấy danh sách Student (có filter + phân trang)</summary>
        Task<PagedResult<StudentDto>> GetAllAsync(StudentFilter filter, CancellationToken ct = default);

        /// <summary>Tạo Student mới (thường gọi khi User đăng ký role Student)</summary>
        Task<StudentDto> CreateAsync(CreateStudentDto dto, CancellationToken ct = default);

        /// <summary>Cập nhật thông tin cá nhân</summary>
        Task UpdateAsync(Guid id, UpdateStudentDto dto, CancellationToken ct = default);

        /// <summary>Xóa mềm</summary>
        Task DeleteAsync(Guid id, CancellationToken ct = default);

        /// <summary>Khôi phục</summary>
        Task RestoreAsync(Guid id, CancellationToken ct = default);


        // ==================== PROFILE ====================

        /// <summary>Lấy profile đầy đủ của Student (kèm stats, level, rank)</summary>
        Task<StudentProfileDto?> GetProfileAsync(Guid studentId, CancellationToken ct = default);

        /// <summary>Cập nhật avatar (Cloudinary)</summary>
        Task UpdateAvatarAsync(Guid studentId, string avatarUrl, string avatarPublicId, CancellationToken ct = default);

        /// <summary>Cập nhật thông tin cá nhân (gender, dob, ...)</summary>
        Task UpdatePersonalInfoAsync(Guid studentId, UpdatePersonalInfoDto dto, CancellationToken ct = default);

    }
}
