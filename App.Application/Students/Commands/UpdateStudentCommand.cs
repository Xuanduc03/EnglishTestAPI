using App.Domain.Entities;
using App.Application.DTO;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Commands
{
    public class UpdateStudentCommand : IRequest<StudentDto>
    {
        public Guid Id { get; set; }
        public string Fullname { get; set; }
        public string? CCCD { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? SBD { get; set; }
        public string? School { get; set; }
        public Guid UpdatedBy { get; set; }
    }

    public class UpdateStudentCommandHandler : IRequestHandler<UpdateStudentCommand, StudentDto>
    {
        private readonly IAppDbContext _context;

        public UpdateStudentCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<StudentDto> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

                if (student == null)
                    throw new Exception($"Student with ID {request.Id} not found");
                if (string.IsNullOrWhiteSpace(request.Fullname) || request.Fullname.Trim().Length > 200)
                    throw new ArgumentException("Họ tên học viên không hợp lệ", nameof(request.Fullname));
                if (request.DateOfBirth.HasValue && request.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
                    throw new ArgumentOutOfRangeException(nameof(request.DateOfBirth), "Ngày sinh không thể ở tương lai");

                // Check if CCCD already exists (excluding current student)
                var cccd = string.IsNullOrWhiteSpace(request.CCCD) ? null : request.CCCD.Trim();
                var sbd = string.IsNullOrWhiteSpace(request.SBD) ? null : request.SBD.Trim();
                if (cccd != null && cccd != student.CCCD)
                {
                    var existingCCCD = await _context.Students
                        .FirstOrDefaultAsync(s => s.CCCD == cccd && s.Id != request.Id, cancellationToken);

                    if (existingCCCD != null)
                        throw new Exception($"Student with CCCD {request.CCCD} already exists");
                }

                // Check if SBD already exists (excluding current student)
                if (sbd != null && sbd != student.SBD)
                {
                    var existingSBD = await _context.Students
                        .FirstOrDefaultAsync(s => s.SBD == sbd && s.Id != request.Id, cancellationToken);

                    if (existingSBD != null)
                        throw new Exception($"Student with SBD {request.SBD} already exists");
                }

                student.Fullname = request.Fullname.Trim();
                student.User.UpdateProfile(student.User.Email, student.Fullname, student.User.Phone);
                student.CCCD = cccd;
                if (!string.IsNullOrWhiteSpace(request.Gender))
                {
                    if (!Enum.TryParse<Gender>(request.Gender, true, out var gender) || !Enum.IsDefined(gender))
                        throw new ArgumentException("Giới tính không hợp lệ", nameof(request.Gender));
                    student.Gender = gender;
                }
                student.DateOfBirth = request.DateOfBirth;
                student.SBD = sbd;
                student.UpdatedAt = DateTime.UtcNow;
                student.UpdatedBy = request.UpdatedBy;
                student.User.UpdatedBy = request.UpdatedBy;

                await _context.SaveChangesAsync(cancellationToken);

                return new StudentDto
                {
                    Id = student.Id,
                    Fullname = student.Fullname,
                    CCCD = student.CCCD,
                    Gender = student.Gender?.ToString(),
                    DateOfBirth = student.DateOfBirth,
                    SBD = student.SBD,
                    UserId = student.UserId,
                    Email = student.User.Email,
                    Phone = student.User.Phone,
                    CreatedAt = student.CreatedAt,
                    UpdatedAt = student.UpdatedAt
                };
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("Database error occurred while updating student", ex);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
