using App.Application.DTO;
using App.Domain.Entities;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Queries
{
    public class GetAllStudentsQuery : IRequest<List<StudentListDto>>
    {
        public string? Search { get; set; }
        public string? Gender { get; set; }
        public string? School { get; set; }
        public bool? HasActiveClasses { get; set; }
        public Guid? ClassId { get; set; }
    }

    public class GetAllStudentsQueryHandler : IRequestHandler<GetAllStudentsQuery, List<StudentListDto>>
    {
        private readonly IAppDbContext _context;

        public GetAllStudentsQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<List<StudentListDto>> Handle(GetAllStudentsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _context.Students
                    .AsNoTracking()
                    .Where(s => s.IsActive && s.User.IsActive)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var search = request.Search.Trim();
                    query = query.Where(s =>
                        s.Fullname.Contains(search) ||
                        (s.SBD != null && s.SBD.Contains(search)) ||
                        (s.CCCD != null && s.CCCD.Contains(search)) ||
                        s.User.Email.Contains(search));
                }

                if (!string.IsNullOrWhiteSpace(request.Gender))
                {
                    if (!Enum.TryParse<Gender>(request.Gender, true, out var gender) || !Enum.IsDefined(gender))
                        throw new ArgumentException("Giới tính không hợp lệ", nameof(request.Gender));
                    query = query.Where(s => s.Gender == gender);
                }

               

                var students = await query
                    .OrderBy(s => s.Fullname)
                    .Select(s => new
                    {
                        s.Id,
                        s.Fullname,
                        s.CCCD,
                        s.Gender,
                        s.DateOfBirth,
                        s.SBD,
                        s.User.Email,
                        s.User.Phone
                    })
                    .ToListAsync(cancellationToken);

                var studentDtos = students.Select(s => new StudentListDto
                {
                    Id = s.Id,
                    Fullname = s.Fullname,
                    CCCD = s.CCCD,
                    Gender = s.Gender?.ToString(),
                    DateOfBirth = s.DateOfBirth,
                    SBD = s.SBD,
                    Email = s.Email,
                    Phone = s.Phone,
                 }).ToList();

                return studentDtos;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
