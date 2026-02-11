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
                    .Include(s => s.User)
                    .Where(s => s.IsActive == false)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.Search))
                {
                    query = query.Where(s =>
                        s.Fullname.Contains(request.Search) ||
                        s.SBD.Contains(request.Search) ||
                        s.CCCD.Contains(request.Search) ||
                        s.School.Contains(request.Search) ||
                        s.User.Email.Contains(request.Search));
                }

                if (!string.IsNullOrEmpty(request.Gender))
                {
                    query = query.Where(s => s.Gender == request.Gender);
                }

                if (!string.IsNullOrEmpty(request.School))
                {
                    query = query.Where(s => s.School == request.School);
                }

              

                var students = await query
                    .OrderBy(s => s.Fullname)
                    .ToListAsync(cancellationToken);

                var studentDtos = students.Select(s => new StudentListDto
                {
                    Id = s.Id,
                    Fullname = s.Fullname,
                    CCCD = s.CCCD,
                    Gender = s.Gender,
                    DateOfBirth = s.DateOfBirth,
                    SBD = s.SBD,
                    School = s.School,
                    Email = s.User?.Email,
                    Phone = s.User?.Phone,
                 }).ToList();

                return studentDtos;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving students: {ex.Message}", ex);
            }
        }
    }
}