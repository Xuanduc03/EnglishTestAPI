using App.Domain.Entities;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Commands
{
    public class DeleteStudentCommand : IRequest<bool>
    {
        public Guid? id { get; set; }
        public List<Guid>? ids { get; set; }
        public Guid DeletedBy { get; set; }
    }

    public class DeleteStudentCommandHandler : IRequestHandler<DeleteStudentCommand, bool>
    {
        private readonly IAppDbContext _context;

        public DeleteStudentCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteStudentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                //1. chuuẩn  hóa id cần xóa
                // Gom tất cả ID từ request.Id (xóa đơn) và request.Ids (xóa nhiều) vào một list duy nhất
                var targets = new List<Guid>();

                if(request.ids != null && request.ids.Any())
                {
                    targets.AddRange(request.ids);
                }

                if(request.id.HasValue && request.id != Guid.Empty)
                {
                    // tránh add trùng nếu id có trong list
                    if (!targets.Contains(request.id.Value))
                    {
                        targets.Add(request.id.Value);
                    }
                }

                if (!targets.Any())
                {
                    return false;
                }
                
                var students = await _context.Students
                    .Where(s => targets.Contains(s.Id))
                    .ToListAsync(cancellationToken);
                if (students.Count == 0) return false;

                var now = DateTime.UtcNow;
                foreach (var student in students)
                {
                    student.IsActive = false;
                    student.IsDeleted = true;
                    student.DeletedAt = now;
                    student.DeletedBy = request.DeletedBy;
                }

                await _context.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
