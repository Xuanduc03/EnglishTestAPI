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
            var exams = await _context.Exams
                .Where(x => !x.IsDeleted
                    && x.Category == ExamCategory.FullTest
                    && x.Status == ExamStatus.Published
                    && x.IsActive)
                .OrderByDescending(x => x.CreatedAt) // Mới nhất lên đầu
                .ProjectTo<ExamSummaryDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return exams;
        }
    }
}
