using App.Application.DTO;
using App.Application.DTOs;
using App.Domain.Entities;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Queries
{
    public class GetStudentByIdQuery : IRequest<StudentDetailDto>
    {
        public Guid Id { get; set; }
    }

    public class GetStudentByIdQueryHandler : IRequestHandler<GetStudentByIdQuery, StudentDetailDto>
    {
        private readonly IAppDbContext _context;

        public GetStudentByIdQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<StudentDetailDto> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
        {
            var studentDto = await _context.Students
                 .AsNoTracking()
                 .Where(s => s.Id == request.Id)
                 .Select(s => new StudentDetailDto
                 {
                     Id = s.Id,
                     Fullname = s.Fullname,
                     CCCD = s.CCCD,
                     Gender = s.Gender,
                     SBD = s.SBD,
                     CreatedAt = s.CreatedAt,
                     UpdatedAt = s.UpdatedAt,
                     UserId = s.UserId,
                     Email = s.User.Email,
                     Phone = s.User.Phone,
                     User = s.User == null ? null : new UserDto
                     {
                         Id = s.User.Id,
                         AvatarUrl = s.User.AvatarUrl,
                         IsActive = s.User.IsActive,
                         LastLogin = s.User.LastLogin
                     }
                 })
                 .FirstOrDefaultAsync(cancellationToken);

            // Kiểm tra null chuẩn
            if (studentDto == null)
            {
                // Ném lỗi KeyNotFound để Middleware trả về 404 Not Found
                throw new KeyNotFoundException($"Không tìm thấy học sinh với ID {request.Id}");
            }

            return studentDto;
        }
    }
}