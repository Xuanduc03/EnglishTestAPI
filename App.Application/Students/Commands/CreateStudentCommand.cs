using App.Domain.Entities;
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

                var checkUserId = ValidateUserAsync(dto, cancellationToken);

                var student = new Student
                {
                    Id = Guid.NewGuid(),
                    Fullname = dto.Fullname,
                    CCCD = dto.CCCD,
                    Gender = dto.Gender,
                    DateOfBirth = dto.DateOfBirth,
                    SBD = dto.SBD,
                    UserId = dto.UserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.Students.Add(student);

                await _context.SaveChangesAsync();

                return student.Id;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("Database error occurred while creating student", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating student: {ex.Message}", ex);
            }
        }

        private async Task ValidateUserAsync(CreateStudentDto dto, CancellationToken ct)
        {
            var userExists = await _context.Users.AnyAsync(x => x.Id == dto.UserId, ct);

            if (!userExists)
            {
                throw new KeyNotFoundException($"User với ID {dto.UserId} không tồn tại.");
            }

            var isStudent = await _context.Students.AnyAsync(x => x.Id == dto.UserId && !x.IsDeleted, ct);

            if (isStudent)
            {
                throw new InvalidOperationException($"User {dto.UserId} đã có hồ sơ học sinh rồi.");
            }

            if (!string.IsNullOrEmpty(dto.CCCD))
            {
                var duplicateCCCD = await _context.Students.AnyAsync(s => s.CCCD == dto.CCCD && !s.IsDeleted, ct);
                if (duplicateCCCD)
                {
                    throw new InvalidOperationException($"CCCD {dto.CCCD} đã tồn tại trong hệ thống.");
                }
            }
            if (!string.IsNullOrEmpty(dto.SBD))
            {
                var duplicateSBD = await _context.Students.AnyAsync(s => s.SBD == dto.SBD && !s.IsDeleted, ct);
                if (duplicateSBD)
                {
                    throw new InvalidOperationException($"Số báo danh {dto.SBD} đã tồn tại.");
                }
            }
        }
    }
}