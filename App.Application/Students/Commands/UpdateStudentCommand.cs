using App.Domain.Entities;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Commands
{
    public class UpdateStudentCommand : IRequest<Student>
    {
        public Guid Id { get; set; }
        public string Fullname { get; set; }
        public string? CCCD { get; set; }
        public string? Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? SBD { get; set; }
        public string? School { get; set; }
        public Guid UpdatedBy { get; set; }
    }

    public class UpdateStudentCommandHandler : IRequestHandler<UpdateStudentCommand, Student>
    {
        private readonly IAppDbContext _context;

        public UpdateStudentCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<Student> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

                if (student == null)
                    throw new Exception($"Student with ID {request.Id} not found");

                // Check if CCCD already exists (excluding current student)
                if (!string.IsNullOrEmpty(request.CCCD) && request.CCCD != student.CCCD)
                {
                    var existingCCCD = await _context.Students
                        .FirstOrDefaultAsync(s => s.CCCD == request.CCCD && s.Id != request.Id, cancellationToken);

                    if (existingCCCD != null)
                        throw new Exception($"Student with CCCD {request.CCCD} already exists");
                }

                // Check if SBD already exists (excluding current student)
                if (!string.IsNullOrEmpty(request.SBD) && request.SBD != student.SBD)
                {
                    var existingSBD = await _context.Students
                        .FirstOrDefaultAsync(s => s.SBD == request.SBD && s.Id != request.Id, cancellationToken);

                    if (existingSBD != null)
                        throw new Exception($"Student with SBD {request.SBD} already exists");
                }

                student.Fullname = request.Fullname;
                student.CCCD = request.CCCD;
                student.Gender = request.Gender;
                student.DateOfBirth = request.DateOfBirth;
                student.SBD = request.SBD;
                student.UpdatedAt = DateTime.UtcNow;

                _context.Students.Update(student);
                await _context.SaveChangesAsync(cancellationToken);

                return student;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("Database error occurred while updating student", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating student: {ex.Message}", ex);
            }
        }
    }
}