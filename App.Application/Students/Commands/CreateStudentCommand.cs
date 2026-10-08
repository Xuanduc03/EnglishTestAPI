using App.Domain.Entities;
using App.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using App.Application.Interfaces;
using App.Application.DTO;

namespace App.Application.Students.Commands
{
    public record CreateStudentCommand(CreateStudentDto data) : IRequest<Guid>;

    public class CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, Guid>
    {
        private readonly IAppDbContext _context;

        public CreateStudentCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var dto = request.data;
                if (string.IsNullOrWhiteSpace(dto.Fullname) || dto.Fullname.Trim().Length > 200)
                    throw new ArgumentException("Họ tên học viên không hợp lệ", nameof(dto.Fullname));
                if (dto.DateOfBirth.HasValue && dto.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
                    throw new ArgumentOutOfRangeException(nameof(dto.DateOfBirth), "Ngày sinh không thể ở tương lai");
                Gender? gender = null;
                if (!string.IsNullOrWhiteSpace(dto.Gender))
                {
                    if (!Enum.TryParse<Gender>(dto.Gender, true, out var parsedGender) || !Enum.IsDefined(parsedGender))
                        throw new ArgumentException("Giới tính không hợp lệ", nameof(dto.Gender));
                    gender = parsedGender;
                }

                var user = await ValidateUserAsync(dto, cancellationToken);
                user.UpdateProfile(user.Email, dto.Fullname, user.Phone);

                var student = new Student
                {
                    Id = Guid.NewGuid(),
                    Fullname = dto.Fullname.Trim(),
                    CCCD = string.IsNullOrWhiteSpace(dto.CCCD) ? null : dto.CCCD.Trim(),
                    Gender = gender,
                    DateOfBirth = dto.DateOfBirth,
                    SBD = string.IsNullOrWhiteSpace(dto.SBD) ? null : dto.SBD.Trim(),
                    UserId = dto.UserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.Students.Add(student);

                await _context.SaveChangesAsync(cancellationToken);

                return student.Id;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("Database error occurred while creating student", ex);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<User> ValidateUserAsync(CreateStudentDto dto, CancellationToken ct)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == dto.UserId, ct);

            if (user == null)
            {
                throw new KeyNotFoundException($"User với ID {dto.UserId} không tồn tại.");
            }
            if (user.Role != UserRole.Student)
                throw new InvalidOperationException("Chỉ tài khoản học viên mới được tạo hồ sơ học viên.");

            var isStudent = await _context.Students.IgnoreQueryFilters().AnyAsync(x => x.UserId == dto.UserId, ct);

            if (isStudent)
            {
                throw new InvalidOperationException($"User {dto.UserId} đã có hồ sơ học sinh rồi.");
            }

            if (!string.IsNullOrEmpty(dto.CCCD))
            {
                var cccd = dto.CCCD.Trim();
                var duplicateCCCD = await _context.Students.AnyAsync(s => s.CCCD == cccd, ct);
                if (duplicateCCCD)
                {
                    throw new InvalidOperationException($"CCCD {dto.CCCD} đã tồn tại trong hệ thống.");
                }
            }
            if (!string.IsNullOrEmpty(dto.SBD))
            {
                var sbd = dto.SBD.Trim();
                var duplicateSBD = await _context.Students.AnyAsync(s => s.SBD == sbd, ct);
                if (duplicateSBD)
                {
                    throw new InvalidOperationException($"Số báo danh {dto.SBD} đã tồn tại.");
                }
            }
            return user;
        }
    }
}
