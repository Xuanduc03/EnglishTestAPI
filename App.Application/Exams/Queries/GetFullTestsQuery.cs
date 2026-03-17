using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Exams.Queries
{
    /// <summary>
    /// Query: lấy danh sách các bài thi full test 
    /// (ExamCategory.FullTest) đã được xuất bản (ExamStatus.Published)
    /// </summary>
    public class GetFullTestsQuery : IRequest<List<ExamSummaryDto>>
    {
        // Có thể thêm tham số phân trang sau nếu cần
    }

    public class GetFullTestsQueryHandler : IRequestHandler<GetFullTestsQuery, List<ExamSummaryDto>>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;

        public GetFullTestsQueryHandler(IAppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<ExamSummaryDto>> Handle(GetFullTestsQuery request, CancellationToken cancellationToken)
        {
            // Lấy danh sách exam thỏa mãn điều kiện
            var exams = await _context.Exams
                .Where(x => !x.IsDeleted
                    && x.Category == ExamCategory.FullTest
                    && x.Status == ExamStatus.Published
                    && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ProjectTo<ExamSummaryDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            if (!exams.Any()) return exams;

            var examIds = exams.Select(e => e.Id).ToList();

            // Đếm số lượng attempt đang InProgress cho mỗi exam
            var inProgressCounts = await _context.ExamAttempts
                .Where(a => examIds.Contains(a.ExamId) && a.Status == ExamAttemptStatus.InProgress || a.Status == ExamAttemptStatus.Submitted)
                .GroupBy(a => a.ExamId)
                .Select(g => new { ExamId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var countDict = inProgressCounts.ToDictionary(x => x.ExamId, x => x.Count);

            // Gán giá trị vào DTO
            foreach (var exam in exams)
            {
                exam.ActiveUserCount = countDict.GetValueOrDefault(exam.Id, 0);
            }

            return exams;
        }
    }
}
