using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Exams.Commands
{
    // B2 : Thêm section (phần thi)
    // Input: ExamId, Name, Instructions, OrderIndex
    // API: POST /api/exams/{examId}/sections
    public class AddExamSectionCommand : IRequest<Guid>
    {
        public Guid ExamId { get; set; }
        public string Name { get; set; }
        public Guid CategoryId { get; set; }
        public string? Instructions { get; set; }
        public int OrderIndex { get; set; }
        public int? TimeLimit { get; set; }
    }

    public class AddExamSectionHandler : IRequestHandler<AddExamSectionCommand, Guid>
    {
        private readonly IAppDbContext _examRepo;

        public AddExamSectionHandler(IAppDbContext examRepo)
        {
            _examRepo = examRepo;
        }

        public async Task<Guid> Handle(AddExamSectionCommand request, CancellationToken cancellationToken)
        {
            // === VALIDATION ===
            if (request.ExamId == Guid.Empty)
                throw new ValidationException("ExamId không hợp lệ");

            var categoryExists = await _examRepo.Categories
                .AnyAsync(x => x.Id == request.CategoryId);

            if (!categoryExists)
                throw new ValidationException("Category không tồn tại");


            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ValidationException("Tên phần thi không được để trống");

            if (request.Name.Length > 100)
                throw new ValidationException("Tên phần thi tối đa 100 ký tự");

            if (request.OrderIndex < 0)
                throw new ValidationException("Thứ tự phần thi phải >= 0");

            if (request.TimeLimit.HasValue && request.TimeLimit.Value <= 0)
                throw new ValidationException("Thời gian phần thi phải lớn hơn 0");


            // === CHECK EXAM EXISTS ===
            var examExists = await _examRepo.Exams
                .AnyAsync(x => x.Id == request.ExamId, cancellationToken);

            if (!examExists)
                throw new Exception($"Không tìm thấy Exam");


            // === CREATE SECTION ===
            var section = new ExamSection
            {
                Id = Guid.NewGuid(),
                ExamId = request.ExamId,
                CategoryId = request.CategoryId,
                Instructions = request.Instructions,
                OrderIndex = request.OrderIndex,
                TimeLimit = request.TimeLimit,
                CreatedAt = DateTime.UtcNow
            };

            _examRepo.ExamSections.Add(section);

            await _examRepo.SaveChangesAsync(cancellationToken);
            return section.Id;
        }
    }

}
